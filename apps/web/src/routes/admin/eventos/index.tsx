import { useQuery } from "@tanstack/react-query";
import { createFileRoute, Link } from "@tanstack/react-router";
import { useState } from "react";
import { StatusPill } from "#/components/ui/StatusPill";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import { EVENT_KIND, EVENT_STATUS } from "#/features/admin/ui/labels";
import { Notice } from "#/features/admin/ui/Notice";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { Pager } from "#/features/admin/ui/Pager";
import { api, unwrap } from "#/lib/api";
import { formatDayMonth, formatDualTime, formatWeekdayShort } from "#/lib/time";

export const Route = createFileRoute("/admin/eventos/")({
	component: EventsList,
});

const SCOPES = {
	upcoming: "Próximos",
	past: "Passados",
	all: "Todos",
} as const;
type Scope = keyof typeof SCOPES;

function EventsList() {
	const [scope, setScope] = useState<Scope>("upcoming");
	const [page, setPage] = useState(1);
	const events = useQuery({
		queryKey: ["events", scope, page],
		queryFn: () =>
			unwrap(
				api.GET("/api/admin/events", {
					params: { query: { scope, Page: page } },
				}),
			),
	});

	return (
		<div className={styles.page}>
			<PageHeader
				title="Eventos"
				subtitle="Jogos e encontros do painel de partidas. Horários sempre no fuso de Dublin."
				actions={
					<Link
						to="/admin/eventos/novo"
						className={`${styles.button} ${styles.primary}`}
					>
						Novo evento
					</Link>
				}
			/>
			<div className={styles.segmented} role="tablist">
				{(Object.keys(SCOPES) as Scope[]).map((key) => (
					<AdminButton
						key={key}
						role="tab"
						aria-selected={scope === key}
						selected={scope === key}
						onClick={() => {
							setScope(key);
							setPage(1);
						}}
					>
						{SCOPES[key]}
					</AdminButton>
				))}
			</div>

			{events.isError && (
				<Notice tone="error">Não foi possível carregar os eventos.</Notice>
			)}
			<div className={styles.tableWrap}>
				<table className={styles.table}>
					<thead>
						<tr>
							<th scope="col">Quando</th>
							<th scope="col">Evento</th>
							<th scope="col">Onde</th>
							<th scope="col">Situação</th>
							<th scope="col">Site</th>
						</tr>
					</thead>
					<tbody>
						{events.data?.items.map((event) => {
							const start = new Date(event.startsAt);
							return (
								<tr key={event.id}>
									<td>
										<strong>
											{formatWeekdayShort(start, "dublin")}{" "}
											{formatDayMonth(start, "dublin")}
										</strong>
										<div className={styles.muted}>{formatDualTime(start)}</div>
									</td>
									<td>
										<Link
											to="/admin/eventos/$id"
											params={{ id: event.id }}
											className={styles.rowLink}
										>
											{event.title}
										</Link>
										<div className={styles.muted}>{EVENT_KIND[event.kind]}</div>
									</td>
									<td>
										{event.place} · {event.cityCode}
									</td>
									<td>{EVENT_STATUS[event.status]}</td>
									<td>
										<StatusPill
											tone={event.isPublished ? "confirmed" : "pending"}
											size="sm"
										>
											{event.isPublished ? "No ar" : "Rascunho"}
										</StatusPill>
									</td>
								</tr>
							);
						})}
					</tbody>
				</table>
				{events.data?.items.length === 0 && (
					<p className={styles.empty}>Nenhum evento aqui.</p>
				)}
			</div>
			{events.data && (
				<Pager
					page={events.data.page}
					pageSize={events.data.pageSize}
					total={events.data.total}
					onPage={setPage}
				/>
			)}
		</div>
	);
}
