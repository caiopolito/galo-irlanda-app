import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import {
	EventForm,
	emptyEvent,
	formToRequest,
} from "#/features/admin/events/EventForm";
import styles from "#/features/admin/ui/admin.module.css";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { api, unwrap } from "#/lib/api";

export const Route = createFileRoute("/admin/eventos/novo")({
	component: NewEvent,
});

function NewEvent() {
	const navigate = useNavigate();
	const queryClient = useQueryClient();
	const create = useMutation({
		mutationFn: (values: typeof emptyEvent) =>
			unwrap(
				api.POST("/api/admin/events", {
					body: { ...formToRequest(values), publish: values.publish },
				}),
			),
		onSuccess: async (created) => {
			await queryClient.invalidateQueries({ queryKey: ["events"] });
			await navigate({ to: "/admin/eventos/$id", params: { id: created.id } });
		},
	});

	return (
		<div className={styles.page}>
			<PageHeader title="Novo evento" />
			<EventForm
				initial={emptyEvent}
				isNew
				saving={create.isPending}
				error={create.error}
				onSubmit={(values) => create.mutate(values)}
			/>
		</div>
	);
}
