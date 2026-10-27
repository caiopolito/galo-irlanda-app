import type { Schemas } from "@galo/api-client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { type FormEvent, useState } from "react";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import { Field, inputClass, textareaClass } from "#/features/admin/ui/Field";
import { Notice } from "#/features/admin/ui/Notice";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { PhotoPicker } from "#/features/admin/ui/PhotoPicker";
import { ApiError, api, unwrap } from "#/lib/api";

export const Route = createFileRoute("/admin/cidades")({ component: Cities });

function Cities() {
	const cities = useQuery({
		queryKey: ["cities"],
		queryFn: () => unwrap(api.GET("/api/admin/cities")),
	});

	return (
		<div className={styles.page}>
			<PageHeader
				title="Cidades"
				subtitle="Os “portões” da seção “Três portões, uma Massa só.”"
			/>
			{cities.data?.map((city) => (
				<CityCard key={`${city.code}-${city.version}`} city={city} />
			))}
			<NewCity />
		</div>
	);
}

function CityCard({ city }: { city: Schemas["ListCitiesItem"] }) {
	const queryClient = useQueryClient();
	const [values, setValues] = useState({
		name: city.name,
		blurb: city.blurb ?? "",
		photoId: city.photoId,
		sortOrder: city.sortOrder.toString(),
		isActive: city.isActive,
	});
	const save = useMutation({
		mutationFn: () =>
			unwrap(
				api.PUT("/api/admin/cities/{code}", {
					params: { path: { code: city.code.trim() } },
					body: {
						name: values.name,
						blurb: values.blurb || null,
						photoId: values.photoId,
						sortOrder: Number(values.sortOrder || 0),
						isActive: values.isActive,
						version: city.version,
					},
				}),
			),
		onSuccess: () => queryClient.invalidateQueries({ queryKey: ["cities"] }),
	});
	const error = save.error instanceof ApiError ? save.error : null;

	const submit = (event: FormEvent) => {
		event.preventDefault();
		save.mutate();
	};

	return (
		<form className={styles.card} onSubmit={submit}>
			<h2 className={styles.cardTitle}>
				{city.code.trim()} · {city.name}
			</h2>
			{error && <Notice tone="error">{error.summary}</Notice>}
			<div className={styles.form}>
				<Field label="Nome" error={error?.field("name")}>
					{(id, invalid) => (
						<input
							id={id}
							required
							className={inputClass(invalid)}
							value={values.name}
							onChange={(e) => setValues({ ...values, name: e.target.value })}
						/>
					)}
				</Field>
				<Field label="Ordem">
					{(id) => (
						<input
							id={id}
							type="number"
							className={inputClass(false)}
							value={values.sortOrder}
							onChange={(e) =>
								setValues({ ...values, sortOrder: e.target.value })
							}
						/>
					)}
				</Field>
				<Field label="Texto" full>
					{(id) => (
						<textarea
							id={id}
							maxLength={400}
							className={textareaClass(false)}
							style={{ minHeight: 80 }}
							value={values.blurb}
							onChange={(e) => setValues({ ...values, blurb: e.target.value })}
						/>
					)}
				</Field>
				<Field label="Foto">
					{() => (
						<PhotoPicker
							value={values.photoId}
							onChange={(photoId) => setValues({ ...values, photoId })}
						/>
					)}
				</Field>
				<label className={styles.checkbox}>
					<input
						type="checkbox"
						checked={values.isActive}
						onChange={(e) =>
							setValues({ ...values, isActive: e.target.checked })
						}
					/>
					Aparece no site
				</label>
			</div>
			<div className={styles.actions}>
				<AdminButton type="submit" variant="primary" busy={save.isPending}>
					Salvar
				</AdminButton>
				{save.isSuccess && <span className={styles.muted}>Salvo.</span>}
			</div>
		</form>
	);
}

function NewCity() {
	const queryClient = useQueryClient();
	const [values, setValues] = useState({ code: "", name: "" });
	const create = useMutation({
		mutationFn: () =>
			unwrap(
				api.POST("/api/admin/cities", {
					body: {
						code: values.code,
						name: values.name,
						blurb: null,
						sortOrder: 99,
					},
				}),
			),
		onSuccess: async () => {
			setValues({ code: "", name: "" });
			await queryClient.invalidateQueries({ queryKey: ["cities"] });
		},
	});
	const error = create.error instanceof ApiError ? create.error : null;

	return (
		<form
			className={styles.card}
			onSubmit={(event) => {
				event.preventDefault();
				create.mutate();
			}}
		>
			<h2 className={styles.cardTitle}>Nova cidade</h2>
			{error && <Notice tone="error">{error.summary}</Notice>}
			<div className={styles.form}>
				<Field
					label="Código"
					hint="Três letras, como os aeroportos: SNN, LMK…"
					error={error?.field("code")}
				>
					{(id, invalid) => (
						<input
							id={id}
							required
							maxLength={3}
							className={inputClass(invalid)}
							value={values.code}
							onChange={(e) =>
								setValues({ ...values, code: e.target.value.toUpperCase() })
							}
						/>
					)}
				</Field>
				<Field label="Nome" error={error?.field("name")}>
					{(id, invalid) => (
						<input
							id={id}
							required
							className={inputClass(invalid)}
							value={values.name}
							onChange={(e) => setValues({ ...values, name: e.target.value })}
						/>
					)}
				</Field>
			</div>
			<div className={styles.actions}>
				<AdminButton type="submit" busy={create.isPending}>
					Adicionar cidade
				</AdminButton>
			</div>
		</form>
	);
}
