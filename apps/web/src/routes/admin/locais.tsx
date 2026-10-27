import type { Schemas } from "@galo/api-client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { type FormEvent, useState } from "react";
import { StatusPill } from "#/components/ui/StatusPill";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import { Field, inputClass, selectClass } from "#/features/admin/ui/Field";
import { Notice } from "#/features/admin/ui/Notice";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { ApiError, api, unwrap } from "#/lib/api";

export const Route = createFileRoute("/admin/locais")({ component: Venues });

type Venue = Schemas["ListVenuesItem"];

function Venues() {
	const venues = useQuery({
		queryKey: ["venues"],
		queryFn: () => unwrap(api.GET("/api/admin/venues")),
	});
	const [editing, setEditing] = useState<Venue | "new" | null>(null);

	return (
		<div className={styles.page}>
			<PageHeader
				title="Locais"
				subtitle="Pubs parceiros e lugares dos encontros."
				actions={
					<AdminButton variant="primary" onClick={() => setEditing("new")}>
						Novo local
					</AdminButton>
				}
			/>
			<div className={styles.split}>
				<div className={styles.tableWrap}>
					<table className={styles.table}>
						<thead>
							<tr>
								<th scope="col">Local</th>
								<th scope="col">Cidade</th>
								<th scope="col" />
							</tr>
						</thead>
						<tbody>
							{venues.data?.map((venue) => (
								<tr key={venue.id}>
									<td>
										<button
											type="button"
											className={styles.rowLink}
											style={{
												background: "none",
												border: 0,
												padding: 0,
												cursor: "pointer",
											}}
											onClick={() => setEditing(venue)}
										>
											{venue.name}
										</button>
										{venue.neighborhood && (
											<div className={styles.muted}>{venue.neighborhood}</div>
										)}
									</td>
									<td>{venue.cityCode}</td>
									<td>
										<span className={styles.badges}>
											{venue.isPartner && (
												<StatusPill tone="boarding" size="sm">
													Parceiro
												</StatusPill>
											)}
											{!venue.isActive && (
												<StatusPill tone="pending" size="sm">
													Inativo
												</StatusPill>
											)}
										</span>
									</td>
								</tr>
							))}
						</tbody>
					</table>
					{venues.data?.length === 0 && (
						<p className={styles.empty}>Nenhum local cadastrado.</p>
					)}
				</div>
				{editing && (
					<VenueForm
						key={editing === "new" ? "new" : `${editing.id}-${editing.version}`}
						venue={editing === "new" ? null : editing}
						onDone={() => setEditing(null)}
					/>
				)}
			</div>
		</div>
	);
}

function VenueForm({
	venue,
	onDone,
}: {
	venue: Venue | null;
	onDone: () => void;
}) {
	const queryClient = useQueryClient();
	const cities = useQuery({
		queryKey: ["cities"],
		queryFn: () => unwrap(api.GET("/api/admin/cities")),
	});
	const [values, setValues] = useState({
		cityCode: venue?.cityCode.trim() ?? "DUB",
		name: venue?.name ?? "",
		neighborhood: venue?.neighborhood ?? "",
		address: venue?.address ?? "",
		mapsUrl: venue?.mapsUrl ?? "",
		websiteUrl: venue?.websiteUrl ?? "",
		isPartner: venue?.isPartner ?? false,
		showsWithSound: venue?.showsWithSound ?? false,
		notes: venue?.notes ?? "",
		isActive: venue?.isActive ?? true,
	});
	const set = <K extends keyof typeof values>(
		key: K,
		value: (typeof values)[K],
	) => setValues((v) => ({ ...v, [key]: value }));
	const nullable = (value: string) =>
		value.trim() === "" ? null : value.trim();

	const save = useMutation({
		mutationFn: () => {
			const body = {
				name: values.name,
				neighborhood: nullable(values.neighborhood),
				address: nullable(values.address),
				mapsUrl: nullable(values.mapsUrl),
				websiteUrl: nullable(values.websiteUrl),
				isPartner: values.isPartner,
				showsWithSound: values.showsWithSound,
				notes: nullable(values.notes),
			};
			return venue
				? unwrap(
						api.PUT("/api/admin/venues/{id}", {
							params: { path: { id: venue.id } },
							body: {
								...body,
								isActive: values.isActive,
								version: venue.version,
							},
						}),
					)
				: unwrap(
						api.POST("/api/admin/venues", {
							body: { ...body, cityCode: values.cityCode },
						}),
					);
		},
		onSuccess: async () => {
			await queryClient.invalidateQueries({ queryKey: ["venues"] });
			onDone();
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
				<h2 className={styles.cardTitle}>
					{venue ? "Editar local" : "Novo local"}
				</h2>
				<AdminButton variant="ghost" onClick={onDone}>
					Fechar
				</AdminButton>
			</div>
			{error && <Notice tone="error">{error.summary}</Notice>}
			{!venue && (
				<Field label="Cidade" error={error?.field("cityCode")}>
					{(id, invalid) => (
						<select
							id={id}
							className={selectClass(invalid)}
							value={values.cityCode}
							onChange={(e) => set("cityCode", e.target.value)}
						>
							{cities.data?.map((city) => (
								<option key={city.code} value={city.code.trim()}>
									{city.name}
								</option>
							))}
						</select>
					)}
				</Field>
			)}
			<Field label="Nome" error={error?.field("name")}>
				{(id, invalid) => (
					<input
						id={id}
						required
						className={inputClass(invalid)}
						value={values.name}
						onChange={(e) => set("name", e.target.value)}
					/>
				)}
			</Field>
			<Field label="Bairro">
				{(id) => (
					<input
						id={id}
						className={inputClass(false)}
						value={values.neighborhood}
						onChange={(e) => set("neighborhood", e.target.value)}
					/>
				)}
			</Field>
			<Field label="Endereço">
				{(id) => (
					<input
						id={id}
						className={inputClass(false)}
						value={values.address}
						onChange={(e) => set("address", e.target.value)}
					/>
				)}
			</Field>
			<Field
				label="Link do mapa"
				error={error?.field("mapsUrl")}
				hint="https://…"
			>
				{(id, invalid) => (
					<input
						id={id}
						type="url"
						className={inputClass(invalid)}
						value={values.mapsUrl}
						onChange={(e) => set("mapsUrl", e.target.value)}
					/>
				)}
			</Field>
			<Field label="Site" error={error?.field("websiteUrl")}>
				{(id, invalid) => (
					<input
						id={id}
						type="url"
						className={inputClass(invalid)}
						value={values.websiteUrl}
						onChange={(e) => set("websiteUrl", e.target.value)}
					/>
				)}
			</Field>
			<Field label="Observações">
				{(id) => (
					<input
						id={id}
						className={inputClass(false)}
						value={values.notes}
						onChange={(e) => set("notes", e.target.value)}
					/>
				)}
			</Field>
			<label className={styles.checkbox}>
				<input
					type="checkbox"
					checked={values.isPartner}
					onChange={(e) => set("isPartner", e.target.checked)}
				/>
				Pub parceiro
			</label>
			<label className={styles.checkbox}>
				<input
					type="checkbox"
					checked={values.showsWithSound}
					onChange={(e) => set("showsWithSound", e.target.checked)}
				/>
				Passa o Galo com som
			</label>
			{venue && (
				<label className={styles.checkbox}>
					<input
						type="checkbox"
						checked={values.isActive}
						onChange={(e) => set("isActive", e.target.checked)}
					/>
					Ativo (desmarque em vez de excluir)
				</label>
			)}
			<div className={styles.actions}>
				<AdminButton type="submit" variant="primary" busy={save.isPending}>
					Salvar local
				</AdminButton>
			</div>
		</form>
	);
}
