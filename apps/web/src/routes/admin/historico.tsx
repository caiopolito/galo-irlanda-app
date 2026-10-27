import { useQuery } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import { formatDateTime } from "#/features/admin/ui/labels";
import { Notice } from "#/features/admin/ui/Notice";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { Pager } from "#/features/admin/ui/Pager";
import { api, unwrap } from "#/lib/api";

export const Route = createFileRoute("/admin/historico")({
	component: History,
});

const ENTITY_TYPES: Record<string, string> = {
	event: "Eventos",
	post: "Notícias",
	photo: "Fotos",
	city: "Cidades",
	venue: "Locais",
	milestone: "Marcos",
	user: "Usuários",
};

const ACTIONS: Record<string, string> = {
	create: "criou",
	update: "editou",
	delete: "excluiu",
	invite: "convidou",
	role_grant: "deu papel",
	role_revoke: "tirou papel",
	disable: "desativou",
	enable: "reativou",
};

/** {"title":["antes","depois"]} → "title: antes → depois". */
function describeChanges(changes: string | null) {
	if (!changes) return null;
	try {
		const parsed = JSON.parse(changes) as Record<string, unknown>;
		return Object.entries(parsed).map(([field, value]) =>
			Array.isArray(value) && value.length === 2
				? `${field}: ${String(value[0] ?? "—")} → ${String(value[1] ?? "—")}`
				: `${field}: ${JSON.stringify(value)}`,
		);
	} catch {
		return [changes];
	}
}

function History() {
	const [entityType, setEntityType] = useState<string | undefined>();
	const [page, setPage] = useState(1);
	const history = useQuery({
		queryKey: ["history", entityType, page],
		queryFn: () =>
			unwrap(
				api.GET("/api/admin/history", {
					params: { query: { entityType, Page: page, PageSize: 30 } },
				}),
			),
	});

	return (
		<div className={styles.page}>
			<PageHeader title="Histórico" subtitle="Quem mudou o quê e quando." />
			<div className={styles.segmented}>
				<AdminButton
					selected={!entityType}
					onClick={() => setEntityType(undefined)}
				>
					Tudo
				</AdminButton>
				{Object.entries(ENTITY_TYPES).map(([key, label]) => (
					<AdminButton
						key={key}
						selected={entityType === key}
						onClick={() => {
							setEntityType(key);
							setPage(1);
						}}
					>
						{label}
					</AdminButton>
				))}
			</div>
			{history.isError && (
				<Notice tone="error">Só admins veem o histórico.</Notice>
			)}
			<div className={styles.tableWrap}>
				<table className={styles.table}>
					<thead>
						<tr>
							<th scope="col">Quando</th>
							<th scope="col">Quem</th>
							<th scope="col">O quê</th>
							<th scope="col">Mudanças</th>
						</tr>
					</thead>
					<tbody>
						{history.data?.items.map((item) => (
							<tr key={item.id}>
								<td className={styles.muted} style={{ whiteSpace: "nowrap" }}>
									{formatDateTime(item.occurredAt)}
								</td>
								<td>
									{item.actorName ?? (
										<span className={styles.muted}>sistema</span>
									)}
								</td>
								<td>
									{ACTIONS[item.action] ?? item.action}{" "}
									{ENTITY_TYPES[item.entityType]?.toLowerCase() ??
										item.entityType}
								</td>
								<td className={styles.muted} style={{ fontSize: 13 }}>
									{describeChanges(item.changes)?.map((line) => (
										<div key={line}>{line}</div>
									))}
								</td>
							</tr>
						))}
					</tbody>
				</table>
				{history.data?.items.length === 0 && (
					<p className={styles.empty}>Nada por aqui ainda.</p>
				)}
			</div>
			{history.data && (
				<Pager
					page={history.data.page}
					pageSize={history.data.pageSize}
					total={history.data.total}
					onPage={setPage}
				/>
			)}
		</div>
	);
}
