import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import {
	EventForm,
	eventToForm,
	formToRequest,
} from "#/features/admin/events/EventForm";
import { useMe } from "#/features/admin/hooks/useMe";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import { Notice } from "#/features/admin/ui/Notice";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { api, unwrap } from "#/lib/api";

export const Route = createFileRoute("/admin/eventos/$id")({
	component: EditEvent,
});

function EditEvent() {
	const { id } = Route.useParams();
	const navigate = useNavigate();
	const queryClient = useQueryClient();
	const me = useMe();
	const [saved, setSaved] = useState(false);

	const event = useQuery({
		queryKey: ["events", "detail", id],
		queryFn: () =>
			unwrap(api.GET("/api/admin/events/{id}", { params: { path: { id } } })),
	});
	const refresh = () => queryClient.invalidateQueries({ queryKey: ["events"] });

	const update = useMutation({
		mutationFn: (values: Parameters<typeof formToRequest>[0]) =>
			unwrap(
				api.PUT("/api/admin/events/{id}", {
					params: { path: { id } },
					body: { ...formToRequest(values), version: event.data?.version ?? 0 },
				}),
			),
		onSuccess: async () => {
			setSaved(true);
			await refresh();
		},
	});
	const setPublished = useMutation({
		mutationFn: (publish: boolean) =>
			unwrap(
				publish
					? api.POST("/api/admin/events/{id}/publish", {
							params: { path: { id } },
						})
					: api.POST("/api/admin/events/{id}/unpublish", {
							params: { path: { id } },
						}),
			),
		onSuccess: refresh,
	});
	const remove = useMutation({
		mutationFn: () =>
			unwrap(
				api.DELETE("/api/admin/events/{id}", { params: { path: { id } } }),
			),
		onSuccess: async () => {
			await refresh();
			await navigate({ to: "/admin/eventos" });
		},
	});

	if (event.isPending) return <p>Carregando…</p>;
	if (event.isError)
		return <Notice tone="error">Evento não encontrado.</Notice>;

	const isAdmin = me.data?.roles.includes("admin");
	const published = event.data.isPublished;

	return (
		<div className={styles.page}>
			<PageHeader
				title={event.data.title}
				subtitle={
					published ? "No ar no site." : "Rascunho: não aparece no site."
				}
				actions={
					<>
						<AdminButton
							variant={published ? "secondary" : "primary"}
							busy={setPublished.isPending}
							onClick={() => setPublished.mutate(!published)}
						>
							{published ? "Tirar do site" : "Publicar no site"}
						</AdminButton>
						{isAdmin && (
							<AdminButton
								variant="danger"
								busy={remove.isPending}
								onClick={() =>
									confirm("Excluir este evento de vez?") && remove.mutate()
								}
							>
								Excluir
							</AdminButton>
						)}
					</>
				}
			/>
			{saved && !update.isPending && !update.isError && (
				<Notice tone="success">Alterações salvas.</Notice>
			)}
			<EventForm
				key={event.data.version}
				initial={eventToForm(event.data)}
				isNew={false}
				saving={update.isPending}
				error={update.error ?? setPublished.error ?? remove.error}
				onSubmit={(values) => update.mutate(values)}
			/>
		</div>
	);
}
