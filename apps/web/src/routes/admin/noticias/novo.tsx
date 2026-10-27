import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import {
	emptyPost,
	PostForm,
	type PostFormValues,
	postFormToRequest,
} from "#/features/admin/posts/PostForm";
import styles from "#/features/admin/ui/admin.module.css";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { api, unwrap } from "#/lib/api";

export const Route = createFileRoute("/admin/noticias/novo")({
	component: NewPost,
});

function NewPost() {
	const navigate = useNavigate();
	const queryClient = useQueryClient();
	const create = useMutation({
		mutationFn: (values: PostFormValues) =>
			unwrap(api.POST("/api/admin/posts", { body: postFormToRequest(values) })),
		onSuccess: async (created) => {
			await queryClient.invalidateQueries({ queryKey: ["posts"] });
			await navigate({ to: "/admin/noticias/$id", params: { id: created.id } });
		},
	});

	return (
		<div className={styles.page}>
			<PageHeader
				title="Nova notícia"
				subtitle="Ela nasce como rascunho. Publique ou agende depois de salvar."
			/>
			<PostForm
				initial={emptyPost}
				submitLabel="Salvar rascunho"
				saving={create.isPending}
				error={create.error}
				onSubmit={(values) => create.mutate(values)}
			/>
		</div>
	);
}
