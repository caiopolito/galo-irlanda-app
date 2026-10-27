import type { Schemas } from "@galo/api-client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { type FormEvent, useState } from "react";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import { Field, inputClass } from "#/features/admin/ui/Field";
import { Notice } from "#/features/admin/ui/Notice";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { ApiError, api, unwrap } from "#/lib/api";

export const Route = createFileRoute("/admin/beneficios")({
	component: Benefits,
});

function Benefits() {
	const benefits = useQuery({
		queryKey: ["benefits"],
		queryFn: () => unwrap(api.GET("/api/admin/benefits")),
	});
	return (
		<div className={styles.page}>
			<PageHeader
				title="Benefícios"
				subtitle="Os cards de “Seus benefícios” na área do sócio."
			/>
			{benefits.data?.map((benefit) => (
				<BenefitRow
					key={`${benefit.id}-${benefit.version}`}
					benefit={benefit}
				/>
			))}
			<BenefitRow benefit={null} />
		</div>
	);
}

function BenefitRow({
	benefit,
}: {
	benefit: Schemas["ListBenefitsItem"] | null;
}) {
	const queryClient = useQueryClient();
	const [values, setValues] = useState({
		headline: benefit?.headline ?? "",
		title: benefit?.title ?? "",
		description: benefit?.description ?? "",
		highlight: benefit?.highlight ?? false,
		sortOrder: benefit?.sortOrder.toString() ?? "0",
		isActive: benefit?.isActive ?? true,
	});
	const set = <K extends keyof typeof values>(
		key: K,
		value: (typeof values)[K],
	) => setValues((v) => ({ ...v, [key]: value }));
	const refresh = () =>
		queryClient.invalidateQueries({ queryKey: ["benefits"] });
	const body = () => ({
		headline: values.headline,
		title: values.title,
		description: values.description || null,
		venueId: null,
		highlight: values.highlight,
		sortOrder: Number(values.sortOrder || 0),
		isActive: values.isActive,
	});

	const save = useMutation({
		mutationFn: () =>
			benefit
				? unwrap(
						api.PUT("/api/admin/benefits/{id}", {
							params: { path: { id: benefit.id } },
							body: { ...body(), version: benefit.version },
						}),
					)
				: unwrap(
						api.POST("/api/admin/benefits", {
							body: { ...body(), version: null },
						}),
					),
		onSuccess: async () => {
			if (!benefit)
				setValues({
					headline: "",
					title: "",
					description: "",
					highlight: false,
					sortOrder: "0",
					isActive: true,
				});
			await refresh();
		},
	});
	const remove = useMutation({
		mutationFn: () =>
			unwrap(
				api.DELETE("/api/admin/benefits/{id}", {
					params: { path: { id: benefit?.id ?? "" } },
				}),
			),
		onSuccess: refresh,
	});
	const error = save.error instanceof ApiError ? save.error : null;

	const submit = (event: FormEvent) => {
		event.preventDefault();
		save.mutate();
	};

	return (
		<form className={styles.card} onSubmit={submit}>
			{!benefit && <h2 className={styles.cardTitle}>Novo benefício</h2>}
			{error && <Notice tone="error">{error.summary}</Notice>}
			<div className={styles.form}>
				<Field
					label="Destaque"
					hint="Texto grande: “10%”, “Mesa”, “Kit”."
					error={error?.field("headline")}
				>
					{(id, invalid) => (
						<input
							id={id}
							required
							maxLength={20}
							className={inputClass(invalid)}
							value={values.headline}
							onChange={(e) => set("headline", e.target.value)}
						/>
					)}
				</Field>
				<Field
					label="Título"
					hint="“no The Pub”, “guardada em jogo grande”."
					error={error?.field("title")}
				>
					{(id, invalid) => (
						<input
							id={id}
							required
							maxLength={80}
							className={inputClass(invalid)}
							value={values.title}
							onChange={(e) => set("title", e.target.value)}
						/>
					)}
				</Field>
				<Field label="Detalhe">
					{(id) => (
						<input
							id={id}
							maxLength={200}
							className={inputClass(false)}
							value={values.description}
							onChange={(e) => set("description", e.target.value)}
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
							onChange={(e) => set("sortOrder", e.target.value)}
						/>
					)}
				</Field>
			</div>
			<div className={styles.actions}>
				<label className={styles.checkbox}>
					<input
						type="checkbox"
						checked={values.highlight}
						onChange={(e) => set("highlight", e.target.checked)}
					/>
					Destaque em âmbar
				</label>
				<label className={styles.checkbox}>
					<input
						type="checkbox"
						checked={values.isActive}
						onChange={(e) => set("isActive", e.target.checked)}
					/>
					Ativo
				</label>
				<AdminButton
					type="submit"
					variant={benefit ? "secondary" : "primary"}
					busy={save.isPending}
				>
					{benefit ? "Salvar" : "Adicionar"}
				</AdminButton>
				{benefit && (
					<AdminButton
						variant="danger"
						busy={remove.isPending}
						onClick={() =>
							confirm("Excluir este benefício?") && remove.mutate()
						}
					>
						Excluir
					</AdminButton>
				)}
			</div>
		</form>
	);
}
