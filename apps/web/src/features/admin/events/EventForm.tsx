import type { Schemas } from "@galo/api-client";
import { useQuery } from "@tanstack/react-query";
import { type FormEvent, useState } from "react";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import {
	Field,
	inputClass,
	selectClass,
	textareaClass,
} from "#/features/admin/ui/Field";
import { EVENT_KIND, EVENT_STATUS } from "#/features/admin/ui/labels";
import { Notice } from "#/features/admin/ui/Notice";
import { PhotoPicker } from "#/features/admin/ui/PhotoPicker";
import { ApiError, api, unwrap } from "#/lib/api";
import { formatDualTime, fromZonedInput, toZonedInput } from "#/lib/time";

const OTHER_PLACE = "__outro__";

export interface EventFormValues {
	kind: Schemas["EventKind"];
	title: string;
	startsAt: string; // datetime-local em Dublin
	cityCode: string;
	venueId: string; // id, "" ou OTHER_PLACE
	venueNote: string;
	opponent: string;
	competition: string;
	homeTrip: string;
	descriptionMd: string;
	coverPhotoId: string | null;
	status: Schemas["EventStatus"];
	capacity: string;
	spotsTaken: string;
	publish: boolean;
}

export const emptyEvent: EventFormValues = {
	kind: "match",
	title: "Galo × ",
	startsAt: "",
	cityCode: "DUB",
	venueId: "",
	venueNote: "",
	opponent: "",
	competition: "",
	homeTrip: "",
	descriptionMd: "",
	coverPhotoId: null,
	status: "scheduled",
	capacity: "",
	spotsTaken: "0",
	publish: false,
};

export function eventToForm(
	event: Schemas["GetEventResponse"],
): EventFormValues {
	return {
		kind: event.kind,
		title: event.title,
		startsAt: toZonedInput(event.startsAt, "dublin"),
		cityCode: event.cityCode.trim(),
		venueId: event.venueId ?? (event.venueNote ? OTHER_PLACE : ""),
		venueNote: event.venueNote ?? "",
		opponent: event.opponent ?? "",
		competition: event.competition ?? "",
		homeTrip: event.homeTrip ?? "",
		descriptionMd: event.descriptionMd ?? "",
		coverPhotoId: event.coverPhotoId,
		status: event.status,
		capacity: event.capacity?.toString() ?? "",
		spotsTaken: event.spotsTaken.toString(),
		publish: event.isPublished,
	};
}

/** Corpo comum de criação e edição, no formato da API. */
export function formToRequest(values: EventFormValues) {
	const nullable = (value: string) =>
		value.trim() === "" ? null : value.trim();
	const isOther = values.venueId === OTHER_PLACE || values.venueId === "";
	return {
		kind: values.kind,
		title: values.title.trim(),
		startsAt: fromZonedInput(values.startsAt, "dublin"),
		cityCode: values.cityCode,
		venueId: isOther ? null : values.venueId,
		venueNote: isOther ? nullable(values.venueNote) : null,
		opponent: values.kind === "match" ? nullable(values.opponent) : null,
		competition: values.kind === "match" ? nullable(values.competition) : null,
		homeTrip: nullable(values.homeTrip),
		descriptionMd: nullable(values.descriptionMd),
		coverPhotoId: values.coverPhotoId,
		status: values.status,
		capacity: values.capacity === "" ? null : Number(values.capacity),
		spotsTaken: Number(values.spotsTaken || 0),
	};
}

interface EventFormProps {
	initial: EventFormValues;
	isNew: boolean;
	saving: boolean;
	error: unknown;
	onSubmit: (values: EventFormValues) => void;
	extraActions?: React.ReactNode;
}

export function EventForm({
	initial,
	isNew,
	saving,
	error,
	onSubmit,
	extraActions,
}: EventFormProps) {
	const [values, setValues] = useState(initial);
	const set = <K extends keyof EventFormValues>(
		key: K,
		value: EventFormValues[K],
	) => setValues((current) => ({ ...current, [key]: value }));
	const apiError = error instanceof ApiError ? error : null;

	const cities = useQuery({
		queryKey: ["cities"],
		queryFn: () => unwrap(api.GET("/api/admin/cities")),
	});
	const venues = useQuery({
		queryKey: ["venues"],
		queryFn: () => unwrap(api.GET("/api/admin/venues")),
	});
	const cityVenues = (venues.data ?? []).filter(
		(v) => v.cityCode.trim() === values.cityCode && v.isActive,
	);

	const submit = (event: FormEvent) => {
		event.preventDefault();
		onSubmit(values);
	};

	const preview = values.startsAt
		? formatDualTime(new Date(fromZonedInput(values.startsAt, "dublin")))
		: null;
	const isMatch = values.kind === "match";

	return (
		<form className={styles.page} onSubmit={submit}>
			{apiError && <Notice tone="error">{apiError.summary}</Notice>}

			<section className={styles.card}>
				<h2 className={styles.cardTitle}>O que e quando</h2>
				<div className={styles.form}>
					<Field label="Tipo">
						{(id) => (
							<select
								id={id}
								className={selectClass(false)}
								value={values.kind}
								onChange={(e) =>
									set("kind", e.target.value as Schemas["EventKind"])
								}
							>
								{Object.entries(EVENT_KIND).map(([value, label]) => (
									<option key={value} value={value}>
										{label}
									</option>
								))}
							</select>
						)}
					</Field>
					<Field
						label="Título"
						error={apiError?.field("title")}
						hint="Como aparece no painel."
					>
						{(id, invalid) => (
							<input
								id={id}
								required
								maxLength={120}
								className={inputClass(invalid)}
								value={values.title}
								onChange={(e) => set("title", e.target.value)}
							/>
						)}
					</Field>
					{isMatch && (
						<>
							<Field label="Adversário" error={apiError?.field("opponent")}>
								{(id, invalid) => (
									<input
										id={id}
										className={inputClass(invalid)}
										value={values.opponent}
										onChange={(e) => set("opponent", e.target.value)}
									/>
								)}
							</Field>
							<Field label="Campeonato">
								{(id) => (
									<input
										id={id}
										className={inputClass(false)}
										placeholder="Brasileirão, Libertadores…"
										value={values.competition}
										onChange={(e) => set("competition", e.target.value)}
									/>
								)}
							</Field>
						</>
					)}
					<Field
						label="Data e hora em Dublin"
						error={apiError?.field("startsAt")}
						hint={preview ?? "Horário local da Irlanda."}
					>
						{(id, invalid) => (
							<input
								id={id}
								type="datetime-local"
								required
								className={inputClass(invalid)}
								value={values.startsAt}
								onChange={(e) => set("startsAt", e.target.value)}
							/>
						)}
					</Field>
					<Field label="Situação">
						{(id) => (
							<select
								id={id}
								className={selectClass(false)}
								value={values.status}
								onChange={(e) =>
									set("status", e.target.value as Schemas["EventStatus"])
								}
							>
								{Object.entries(EVENT_STATUS).map(([value, label]) => (
									<option key={value} value={value}>
										{label}
									</option>
								))}
							</select>
						)}
					</Field>
				</div>
			</section>

			<section className={styles.card}>
				<h2 className={styles.cardTitle}>Onde</h2>
				<div className={styles.form}>
					<Field label="Cidade" error={apiError?.field("cityCode")}>
						{(id, invalid) => (
							<select
								id={id}
								className={selectClass(invalid)}
								value={values.cityCode}
								onChange={(e) =>
									setValues((v) => ({
										...v,
										cityCode: e.target.value,
										venueId: "",
									}))
								}
							>
								{cities.data?.map((city) => (
									<option key={city.code} value={city.code.trim()}>
										{city.name}
									</option>
								))}
							</select>
						)}
					</Field>
					<Field
						label="Local"
						error={apiError?.field("venueId")}
						hint="Cadastre pubs e lugares em Locais."
					>
						{(id, invalid) => (
							<select
								id={id}
								className={selectClass(invalid)}
								value={values.venueId}
								onChange={(e) => set("venueId", e.target.value)}
							>
								<option value="">Escolha…</option>
								{cityVenues.map((venue) => (
									<option key={venue.id} value={venue.id}>
										{venue.name}
										{venue.neighborhood ? ` (${venue.neighborhood})` : ""}
									</option>
								))}
								<option value={OTHER_PLACE}>Outro / a definir</option>
							</select>
						)}
					</Field>
					{(values.venueId === OTHER_PLACE || values.venueId === "") && (
						<Field
							label="Descreva o local"
							error={apiError?.field("venueNote")}
						>
							{(id, invalid) => (
								<input
									id={id}
									className={inputClass(invalid)}
									placeholder="Phoenix Park, casa do fulano, a definir…"
									value={values.venueNote}
									onChange={(e) => set("venueNote", e.target.value)}
								/>
							)}
						</Field>
					)}
					<Field label="Volta pra casa" hint="Ônibus noturno, carona…">
						{(id) => (
							<input
								id={id}
								className={inputClass(false)}
								value={values.homeTrip}
								onChange={(e) => set("homeTrip", e.target.value)}
							/>
						)}
					</Field>
				</div>
			</section>

			<section className={styles.card}>
				<h2 className={styles.cardTitle}>Detalhes</h2>
				<div className={styles.form}>
					<Field label="Vagas" hint="Deixe vazio se não houver limite.">
						{(id) => (
							<input
								id={id}
								type="number"
								min={1}
								className={inputClass(false)}
								value={values.capacity}
								onChange={(e) => set("capacity", e.target.value)}
							/>
						)}
					</Field>
					<Field
						label="Confirmados / vagas ocupadas"
						error={apiError?.field("spotsTaken")}
					>
						{(id, invalid) => (
							<input
								id={id}
								type="number"
								min={0}
								className={inputClass(invalid)}
								value={values.spotsTaken}
								onChange={(e) => set("spotsTaken", e.target.value)}
							/>
						)}
					</Field>
					<Field
						label="Foto de capa"
						full
						hint="Usada no destaque do próximo jogo."
					>
						{() => (
							<PhotoPicker
								value={values.coverPhotoId}
								onChange={(id) => set("coverPhotoId", id)}
							/>
						)}
					</Field>
					<Field label="Descrição" full hint="Markdown.">
						{(id) => (
							<textarea
								id={id}
								className={textareaClass(false)}
								value={values.descriptionMd}
								onChange={(e) => set("descriptionMd", e.target.value)}
							/>
						)}
					</Field>
					{isNew && (
						<label className={`${styles.checkbox} ${styles.full}`}>
							<input
								type="checkbox"
								checked={values.publish}
								onChange={(e) => set("publish", e.target.checked)}
							/>
							Publicar no site ao salvar
						</label>
					)}
				</div>
			</section>

			<div className={styles.actions}>
				<AdminButton type="submit" variant="primary" busy={saving}>
					{isNew ? "Criar evento" : "Salvar alterações"}
				</AdminButton>
				{extraActions}
			</div>
		</form>
	);
}
