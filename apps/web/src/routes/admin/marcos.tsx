import type { Schemas } from "@galo/api-client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { type FormEvent, useState } from "react";
import { useMe } from "#/features/admin/hooks/useMe";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import { Field, inputClass } from "#/features/admin/ui/Field";
import { Notice } from "#/features/admin/ui/Notice";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { ApiError, api, unwrap } from "#/lib/api";

export const Route = createFileRoute("/admin/marcos")({
	component: Milestones,
});

function Milestones() {
	const milestones = useQuery({
		queryKey: ["milestones"],
		queryFn: () => unwrap(api.GET("/api/admin/milestones")),
	});

	return (
		<div className={styles.page}>
			<PageHeader
				title="Marcos"
				subtitle="A seção “Escalas”. Publique só fatos conferidos em fonte oficial."
			/>
			{milestones.data?.map((milestone) => (
				<MilestoneRow
					key={`${milestone.id}-${milestone.version}`}
					milestone={milestone}
				/>
			))}
			<MilestoneRow milestone={null} />
		</div>
	);
}

function MilestoneRow({
	milestone,
}: {
	milestone: Schemas["ListMilestonesItem"] | null;
}) {
	const queryClient = useQueryClient();
	const me = useMe();
	const [values, setValues] = useState({
		year: milestone?.year.toString() ?? "",
		text: milestone?.text ?? "",
		isPublished: milestone?.isPublished ?? false,
	});
	const refresh = () =>
		queryClient.invalidateQueries({ queryKey: ["milestones"] });

	const save = useMutation({
		mutationFn: () => {
			const body = {
				year: Number(values.year),
				text: values.text,
				photoId: null,
				sortOrder: 0,
				isPublished: values.isPublished,
			};
			return milestone
				? unwrap(
						api.PUT("/api/admin/milestones/{id}", {
							params: { path: { id: milestone.id } },
							body: { ...body, version: milestone.version },
						}),
					)
				: unwrap(api.POST("/api/admin/milestones", { body }));
		},
		onSuccess: async () => {
			if (!milestone) setValues({ year: "", text: "", isPublished: false });
			await refresh();
		},
	});
	const remove = useMutation({
		mutationFn: () =>
			unwrap(
				api.DELETE("/api/admin/milestones/{id}", {
					params: { path: { id: milestone?.id ?? "" } },
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
			{!milestone && <h2 className={styles.cardTitle}>Novo marco</h2>}
			{error && <Notice tone="error">{error.summary}</Notice>}
			<div
				className={styles.form}
				style={{ gridTemplateColumns: "120px minmax(0, 1fr)" }}
			>
				<Field label="Ano" error={error?.field("year")}>
					{(id, invalid) => (
						<input
							id={id}
							type="number"
							required
							min={1900}
							max={2100}
							className={inputClass(invalid)}
							value={values.year}
							onChange={(e) => setValues({ ...values, year: e.target.value })}
						/>
					)}
				</Field>
				<Field label="Texto" error={error?.field("text")}>
					{(id, invalid) => (
						<input
							id={id}
							required
							maxLength={200}
							className={inputClass(invalid)}
							value={values.text}
							onChange={(e) => setValues({ ...values, text: e.target.value })}
						/>
					)}
				</Field>
			</div>
			<div className={styles.actions}>
				<label className={styles.checkbox}>
					<input
						type="checkbox"
						checked={values.isPublished}
						onChange={(e) =>
							setValues({ ...values, isPublished: e.target.checked })
						}
					/>
					Publicado (fato conferido)
				</label>
				<AdminButton
					type="submit"
					variant={milestone ? "secondary" : "primary"}
					busy={save.isPending}
				>
					{milestone ? "Salvar" : "Adicionar"}
				</AdminButton>
				{milestone && me.data?.roles.includes("admin") && (
					<AdminButton
						variant="danger"
						busy={remove.isPending}
						onClick={() => confirm("Excluir este marco?") && remove.mutate()}
					>
						Excluir
					</AdminButton>
				)}
			</div>
		</form>
	);
}
