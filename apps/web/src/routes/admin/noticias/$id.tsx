import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import { useMe } from "#/features/admin/hooks/useMe";
import {
	PostForm,
	type PostFormValues,
	postFormToRequest,
	postToForm,
} from "#/features/admin/posts/PostForm";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import { Field, inputClass } from "#/features/admin/ui/Field";
import { formatDateTime, POST_STATUS } from "#/features/admin/ui/labels";
import { Notice } from "#/features/admin/ui/Notice";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { api, unwrap } from "#/lib/api";
import { fromZonedInput } from "#/lib/time";

export const Route = createFileRoute("/admin/noticias/$id")({
	component: EditPost,
});

function EditPost() {
	const { id } = Route.useParams();
	const navigate = useNavigate();
	const queryClient = useQueryClient();
	const me = useMe();
	const [message, setMessage] = useState<string | null>(null);
	const [scheduleAt, setScheduleAt] = useState("");

	const post = useQuery({
		queryKey: ["posts", "detail", id],
		queryFn: () =>
			unwrap(api.GET("/api/admin/posts/{id}", { params: { path: { id } } })),
	});
	const done = (text: string) => async () => {
		setMessage(text);
		await queryClient.invalidateQueries({ queryKey: ["posts"] });
	};
	const path = { params: { path: { id } } };

	const update = useMutation({
		mutationFn: (values: PostFormValues) =>
			unwrap(
				api.PUT("/api/admin/posts/{id}", {
					...path,
					body: {
						...postFormToRequest(values),
						version: post.data?.version ?? 0,
					},
				}),
			),
		onSuccess: done("Alterações salvas."),
	});
	const publish = useMutation({
		mutationFn: (publishAt: string | null) =>
			unwrap(
				api.POST("/api/admin/posts/{id}/publish", {
					...path,
					body: { publishAt },
				}),
			),
		onSuccess: done("Notícia publicada."),
	});
	const unpublish = useMutation({
		mutationFn: () => unwrap(api.POST("/api/admin/posts/{id}/unpublish", path)),
		onSuccess: done("Notícia voltou para rascunho."),
	});
	const archive = useMutation({
		mutationFn: () => unwrap(api.POST("/api/admin/posts/{id}/archive", path)),
		onSuccess: done("Notícia arquivada: saiu do site."),
	});
	const remove = useMutation({
		mutationFn: () => unwrap(api.DELETE("/api/admin/posts/{id}", path)),
		onSuccess: async () => {
			await queryClient.invalidateQueries({ queryKey: ["posts"] });
			await navigate({ to: "/admin/noticias" });
		},
	});

	if (post.isPending) return <p>Carregando…</p>;
	if (post.isError)
		return <Notice tone="error">Notícia não encontrada.</Notice>;

	const { status, publishedAt } = post.data;
	const scheduled =
		status === "published" &&
		publishedAt &&
		new Date(publishedAt).getTime() > Date.now();
	const subtitle = scheduled
		? `Agendada para ${formatDateTime(publishedAt)} (Dublin).`
		: status === "published" && publishedAt
			? `Publicada em ${formatDateTime(publishedAt)}.`
			: POST_STATUS[status];

	return (
		<div className={styles.page}>
			<PageHeader
				title={post.data.title}
				subtitle={subtitle}
				actions={
					me.data?.roles.includes("admin") && (
						<AdminButton
							variant="danger"
							busy={remove.isPending}
							onClick={() =>
								confirm(
									"Excluir esta notícia de vez? Para só tirar do site, arquive.",
								) && remove.mutate()
							}
						>
							Excluir
						</AdminButton>
					)
				}
			/>
			{message && <Notice tone="success">{message}</Notice>}
			{(publish.error || unpublish.error || archive.error || remove.error) && (
				<Notice tone="error">Não foi possível concluir a ação.</Notice>
			)}

			<section className={styles.card}>
				<h2 className={styles.cardTitle}>Publicação</h2>
				<div className={styles.actions}>
					{status !== "published" || scheduled ? (
						<AdminButton
							variant="primary"
							busy={publish.isPending}
							onClick={() => publish.mutate(null)}
						>
							Publicar agora
						</AdminButton>
					) : (
						<AdminButton
							busy={unpublish.isPending}
							onClick={() => unpublish.mutate()}
						>
							Voltar para rascunho
						</AdminButton>
					)}
					{status !== "archived" && (
						<AdminButton
							busy={archive.isPending}
							onClick={() => archive.mutate()}
						>
							Arquivar
						</AdminButton>
					)}
				</div>
				<div className={styles.form}>
					<Field label="Agendar para (horário de Dublin)">
						{(fieldId) => (
							<input
								id={fieldId}
								type="datetime-local"
								className={inputClass(false)}
								value={scheduleAt}
								onChange={(e) => setScheduleAt(e.target.value)}
							/>
						)}
					</Field>
					<div className={styles.field} style={{ justifyContent: "flex-end" }}>
						<AdminButton
							disabled={!scheduleAt}
							busy={publish.isPending}
							onClick={() =>
								publish.mutate(fromZonedInput(scheduleAt, "dublin"))
							}
						>
							Agendar
						</AdminButton>
					</div>
				</div>
			</section>

			<PostForm
				key={post.data.version}
				initial={postToForm(post.data)}
				submitLabel="Salvar alterações"
				saving={update.isPending}
				error={update.error}
				onSubmit={(values) => update.mutate(values)}
			/>
		</div>
	);
}
