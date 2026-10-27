import type { Schemas } from "@galo/api-client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { type FormEvent, useState } from "react";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import { Field, inputClass } from "#/features/admin/ui/Field";
import { Notice } from "#/features/admin/ui/Notice";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { ApiError, api, apiBaseUrl, unwrap } from "#/lib/api";

export const Route = createFileRoute("/admin/documentos")({
	component: Documents,
});

function Documents() {
	const documents = useQuery({
		queryKey: ["documents"],
		queryFn: () => unwrap(api.GET("/api/admin/documents")),
	});
	return (
		<div className={styles.page}>
			<PageHeader
				title="Documentos"
				subtitle="Estatuto, atas e regras. Só sócios logados baixam; publique para aparecer."
			/>
			<UploadForm />
			<div className={styles.tableWrap}>
				<table className={styles.table}>
					<thead>
						<tr>
							<th scope="col">Documento</th>
							<th scope="col">Ordem</th>
							<th scope="col">Publicado</th>
							<th scope="col" />
						</tr>
					</thead>
					<tbody>
						{documents.data?.map((doc) => (
							<DocumentRow key={`${doc.id}-${doc.version}`} doc={doc} />
						))}
					</tbody>
				</table>
				{documents.data?.length === 0 && (
					<p className={styles.empty}>Nenhum documento ainda.</p>
				)}
			</div>
		</div>
	);
}

function UploadForm() {
	const queryClient = useQueryClient();
	const [title, setTitle] = useState("");
	const [file, setFile] = useState<File | null>(null);
	const upload = useMutation({
		mutationFn: async () => {
			const body = new FormData();
			body.append("file", file as File);
			body.append("title", title);
			const response = await fetch(`${apiBaseUrl}/api/admin/documents`, {
				method: "POST",
				body,
				credentials: "include",
				headers: { "X-Galo-Csrf": "1" },
			});
			if (!response.ok)
				throw new ApiError(
					response.status,
					await response.json().catch(() => ({})),
				);
		},
		onSuccess: async () => {
			setTitle("");
			setFile(null);
			await queryClient.invalidateQueries({ queryKey: ["documents"] });
		},
	});
	const error = upload.error instanceof ApiError ? upload.error : null;

	const submit = (event: FormEvent) => {
		event.preventDefault();
		upload.mutate();
	};

	return (
		<form className={styles.card} onSubmit={submit}>
			<h2 className={styles.cardTitle}>Enviar documento</h2>
			{error && (
				<Notice tone="error">
					{error.field("file") ?? error.field("title") ?? error.summary}
				</Notice>
			)}
			<div className={styles.form}>
				<Field label="Título">
					{(id) => (
						<input
							id={id}
							required
							maxLength={120}
							className={inputClass(false)}
							value={title}
							onChange={(e) => setTitle(e.target.value)}
						/>
					)}
				</Field>
				<Field
					label="Arquivo"
					hint="PDF, Word (.docx), JPEG ou PNG, até 20 MB."
				>
					{(id) => (
						<input
							id={id}
							type="file"
							required
							accept="application/pdf,.docx,image/jpeg,image/png"
							className={inputClass(false)}
							onChange={(e) => setFile(e.target.files?.[0] ?? null)}
						/>
					)}
				</Field>
			</div>
			<div className={styles.actions}>
				<AdminButton
					type="submit"
					variant="primary"
					busy={upload.isPending}
					disabled={!file}
				>
					Enviar
				</AdminButton>
			</div>
		</form>
	);
}

function DocumentRow({ doc }: { doc: Schemas["ManageDocumentsItem"] }) {
	const queryClient = useQueryClient();
	const [values, setValues] = useState({
		title: doc.title,
		sortOrder: doc.sortOrder.toString(),
		isPublished: doc.isPublished,
	});
	const refresh = () =>
		queryClient.invalidateQueries({ queryKey: ["documents"] });
	const save = useMutation({
		mutationFn: () =>
			unwrap(
				api.PUT("/api/admin/documents/{id}", {
					params: { path: { id: doc.id } },
					body: {
						title: values.title,
						sortOrder: Number(values.sortOrder || 0),
						isPublished: values.isPublished,
						version: doc.version,
					},
				}),
			),
		onSuccess: refresh,
	});
	const remove = useMutation({
		mutationFn: () =>
			unwrap(
				api.DELETE("/api/admin/documents/{id}", {
					params: { path: { id: doc.id } },
				}),
			),
		onSuccess: refresh,
	});

	return (
		<tr>
			<td>
				<input
					className={inputClass(false)}
					value={values.title}
					onChange={(e) => setValues({ ...values, title: e.target.value })}
					aria-label="Título"
				/>
				<a
					href={`${apiBaseUrl}/api/admin/documents/${doc.id}/download`}
					className={styles.muted}
					style={{ fontSize: 13 }}
				>
					{doc.fileName} · {Math.ceil(doc.sizeBytes / 1024)} KB
				</a>
			</td>
			<td style={{ width: 90 }}>
				<input
					type="number"
					className={inputClass(false)}
					value={values.sortOrder}
					onChange={(e) => setValues({ ...values, sortOrder: e.target.value })}
					aria-label="Ordem"
				/>
			</td>
			<td>
				<label className={styles.checkbox}>
					<input
						type="checkbox"
						checked={values.isPublished}
						onChange={(e) =>
							setValues({ ...values, isPublished: e.target.checked })
						}
					/>
					Sócios veem
				</label>
			</td>
			<td>
				<div className={styles.actions}>
					<AdminButton busy={save.isPending} onClick={() => save.mutate()}>
						Salvar
					</AdminButton>
					<AdminButton
						variant="danger"
						busy={remove.isPending}
						onClick={() =>
							confirm("Excluir este documento?") && remove.mutate()
						}
					>
						Excluir
					</AdminButton>
				</div>
			</td>
		</tr>
	);
}
