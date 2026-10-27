import type { Schemas } from "@galo/api-client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { type FormEvent, useState } from "react";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import { Field, inputClass, selectClass } from "#/features/admin/ui/Field";
import {
	CONSENT_BASIS,
	formatDateTime,
	PHOTO_VISIBILITY,
} from "#/features/admin/ui/labels";
import { Notice } from "#/features/admin/ui/Notice";
import { ApiError, api, photoUrl, unwrap } from "#/lib/api";
import { PhotoTags } from "./PhotoTags";

type Photo = Schemas["GetPhotoResponse"];

/** Painel de edição de uma foto: descrição, consentimento (GDPR) e onde ela aparece. */
export function PhotoEditor({
	id,
	onClose,
}: {
	id: string;
	onClose: () => void;
}) {
	const photo = useQuery({
		queryKey: ["photos", "detail", id],
		queryFn: () =>
			unwrap(api.GET("/api/admin/photos/{id}", { params: { path: { id } } })),
	});

	if (photo.isPending) return <p>Carregando…</p>;
	if (photo.isError) return <Notice tone="error">Foto não encontrada.</Notice>;
	return (
		<PhotoEditorForm
			key={photo.data.version}
			photo={photo.data}
			onClose={onClose}
		/>
	);
}

function PhotoEditorForm({
	photo,
	onClose,
}: {
	photo: Photo;
	onClose: () => void;
}) {
	const queryClient = useQueryClient();
	const [values, setValues] = useState({
		altText: photo.altText ?? "",
		title: photo.title ?? "",
		caption: photo.caption ?? "",
		takenOn: photo.takenOn ?? "",
		visibility: photo.visibility,
		consentBasis: photo.consentBasis,
		consentNote: photo.consentNote ?? "",
		wallFeatured: photo.wallFeatured,
		wallSortOrder: photo.wallSortOrder?.toString() ?? "",
	});
	const set = <K extends keyof typeof values>(
		key: K,
		value: (typeof values)[K],
	) => setValues((current) => ({ ...current, [key]: value }));
	const nullable = (value: string) =>
		value.trim() === "" ? null : value.trim();
	const path = { params: { path: { id: photo.id } } };

	const save = useMutation({
		mutationFn: () =>
			unwrap(
				api.PUT("/api/admin/photos/{id}", {
					...path,
					body: {
						altText: nullable(values.altText),
						title: nullable(values.title),
						caption: nullable(values.caption),
						takenOn: nullable(values.takenOn),
						eventId: null,
						visibility: values.visibility,
						consentBasis: values.consentBasis,
						consentNote: nullable(values.consentNote),
						wallFeatured: values.wallFeatured,
						wallSortOrder:
							values.wallSortOrder === "" ? null : Number(values.wallSortOrder),
						version: photo.version,
					},
				}),
			),
		onSuccess: () => queryClient.invalidateQueries({ queryKey: ["photos"] }),
	});
	const remove = useMutation({
		mutationFn: () => unwrap(api.DELETE("/api/admin/photos/{id}", path)),
		onSuccess: async () => {
			await queryClient.invalidateQueries({ queryKey: ["photos"] });
			onClose();
		},
	});
	const error = save.error instanceof ApiError ? save.error : null;

	const submit = (event: FormEvent) => {
		event.preventDefault();
		save.mutate();
	};

	return (
		<form className={styles.card} onSubmit={submit}>
			<div className={styles.pageHeader}>
				<h2 className={styles.cardTitle}>Editar foto</h2>
				<AdminButton variant="ghost" onClick={onClose}>
					Fechar
				</AdminButton>
			</div>
			<img
				src={photoUrl(photo.baseUrl, 1024)}
				alt={photo.altText ?? ""}
				style={{
					aspectRatio: `${photo.width} / ${photo.height}`,
					objectFit: "contain",
				}}
				className={styles.thumb}
			/>
			{error && <Notice tone="error">{error.summary}</Notice>}
			{save.isSuccess && !save.isPending && (
				<Notice tone="success">Foto salva.</Notice>
			)}

			<Field
				label="Descrição (texto alternativo)"
				error={error?.field("altText")}
				hint="O que aparece na foto, para quem não enxerga. Obrigatória para foto pública."
			>
				{(fieldId, invalid) => (
					<input
						id={fieldId}
						maxLength={300}
						className={inputClass(invalid)}
						value={values.altText}
						onChange={(e) => set("altText", e.target.value)}
					/>
				)}
			</Field>
			<Field label="Título" hint="Em negrito no mural: “Gol aos 44.”">
				{(fieldId) => (
					<input
						id={fieldId}
						maxLength={120}
						className={inputClass(false)}
						value={values.title}
						onChange={(e) => set("title", e.target.value)}
					/>
				)}
			</Field>
			<Field label="Legenda">
				{(fieldId) => (
					<input
						id={fieldId}
						maxLength={300}
						className={inputClass(false)}
						value={values.caption}
						onChange={(e) => set("caption", e.target.value)}
					/>
				)}
			</Field>
			<Field label="Data da foto">
				{(fieldId) => (
					<input
						id={fieldId}
						type="date"
						className={inputClass(false)}
						value={values.takenOn}
						onChange={(e) => set("takenOn", e.target.value)}
					/>
				)}
			</Field>

			<Field
				label="Consentimento"
				error={error?.field("consentBasis")}
				hint={
					photo.consentConfirmedAt
						? `Confirmado em ${formatDateTime(photo.consentConfirmedAt)}.`
						: "Quem aparece na foto concordou em ela ir para o site?"
				}
			>
				{(fieldId, invalid) => (
					<select
						id={fieldId}
						className={selectClass(invalid)}
						value={values.consentBasis}
						onChange={(e) => {
							const consentBasis = e.target.value as Schemas["ConsentBasis"];
							// Consentimento retirado: a foto sai do site na hora.
							setValues((v) =>
								consentBasis === "withdrawn"
									? {
											...v,
											consentBasis,
											visibility: "private",
											wallFeatured: false,
										}
									: { ...v, consentBasis },
							);
						}}
					>
						{Object.entries(CONSENT_BASIS).map(([value, label]) => (
							<option key={value} value={value}>
								{label}
							</option>
						))}
					</select>
				)}
			</Field>
			<Field
				label="Observação do consentimento"
				hint="Ex.: “ok no grupo do WhatsApp em 02/11”."
			>
				{(fieldId) => (
					<input
						id={fieldId}
						maxLength={500}
						className={inputClass(false)}
						value={values.consentNote}
						onChange={(e) => set("consentNote", e.target.value)}
					/>
				)}
			</Field>
			<Field label="Quem vê" error={error?.field("visibility")}>
				{(fieldId, invalid) => (
					<select
						id={fieldId}
						className={selectClass(invalid)}
						value={values.visibility}
						onChange={(e) =>
							set("visibility", e.target.value as Schemas["PhotoVisibility"])
						}
					>
						{Object.entries(PHOTO_VISIBILITY).map(([value, label]) => (
							<option key={value} value={value}>
								{label}
							</option>
						))}
					</select>
				)}
			</Field>
			<label className={styles.checkbox}>
				<input
					type="checkbox"
					checked={values.wallFeatured}
					disabled={values.visibility !== "public"}
					onChange={(e) => set("wallFeatured", e.target.checked)}
				/>
				Mostrar no mural da home (Bagagem de mão)
			</label>
			{error?.field("wallFeatured") && (
				<span className={styles.error}>{error.field("wallFeatured")}</span>
			)}
			{values.wallFeatured && (
				<Field label="Posição no mural" hint="1 aparece primeiro.">
					{(fieldId) => (
						<input
							id={fieldId}
							type="number"
							min={1}
							className={inputClass(false)}
							value={values.wallSortOrder}
							onChange={(e) => set("wallSortOrder", e.target.value)}
						/>
					)}
				</Field>
			)}

			<PhotoTags photoId={photo.id} />

			<div className={styles.actions}>
				<AdminButton type="submit" variant="primary" busy={save.isPending}>
					Salvar foto
				</AdminButton>
				<AdminButton
					variant="danger"
					busy={remove.isPending}
					onClick={() =>
						confirm("Excluir esta foto? Ela sai do site na hora.") &&
						remove.mutate()
					}
				>
					Excluir
				</AdminButton>
			</div>
		</form>
	);
}
