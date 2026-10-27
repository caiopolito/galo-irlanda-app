import { useQuery } from "@tanstack/react-query";
import { createFileRoute, Link } from "@tanstack/react-router";
import { useState } from "react";
import { StatusPill, type StatusTone } from "#/components/ui/StatusPill";
import styles from "#/features/admin/ui/admin.module.css";
import { formatDateTime, POST_STATUS } from "#/features/admin/ui/labels";
import { Notice } from "#/features/admin/ui/Notice";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { Pager } from "#/features/admin/ui/Pager";
import { api, unwrap } from "#/lib/api";

export const Route = createFileRoute("/admin/noticias/")({
	component: PostsList,
});

function PostsList() {
	const [page, setPage] = useState(1);
	const posts = useQuery({
		queryKey: ["posts", page],
		queryFn: () =>
			unwrap(
				api.GET("/api/admin/posts", { params: { query: { Page: page } } }),
			),
	});
	const now = Date.now();

	return (
		<div className={styles.page}>
			<PageHeader
				title="Notícias"
				subtitle="A seção “Da Torcida”. As três mais recentes aparecem na home."
				actions={
					<Link
						to="/admin/noticias/novo"
						className={`${styles.button} ${styles.primary}`}
					>
						Nova notícia
					</Link>
				}
			/>
			{posts.isError && (
				<Notice tone="error">Não foi possível carregar as notícias.</Notice>
			)}
			<div className={styles.tableWrap}>
				<table className={styles.table}>
					<thead>
						<tr>
							<th scope="col">Título</th>
							<th scope="col">Categoria</th>
							<th scope="col">Situação</th>
							<th scope="col">Publicação</th>
						</tr>
					</thead>
					<tbody>
						{posts.data?.items.map((post) => {
							const scheduled =
								post.status === "published" &&
								post.publishedAt &&
								new Date(post.publishedAt).getTime() > now;
							const tone: StatusTone =
								post.status === "published"
									? scheduled
										? "muted"
										: "confirmed"
									: "pending";
							return (
								<tr key={post.id}>
									<td>
										<Link
											to="/admin/noticias/$id"
											params={{ id: post.id }}
											className={styles.rowLink}
										>
											{post.title}
										</Link>
									</td>
									<td>
										{post.category ?? <span className={styles.muted}>—</span>}
									</td>
									<td>
										<StatusPill tone={tone} size="sm">
											{scheduled ? "Agendada" : POST_STATUS[post.status]}
										</StatusPill>
									</td>
									<td className={styles.muted}>
										{post.publishedAt ? formatDateTime(post.publishedAt) : "—"}
									</td>
								</tr>
							);
						})}
					</tbody>
				</table>
				{posts.data?.items.length === 0 && (
					<p className={styles.empty}>Nenhuma notícia ainda.</p>
				)}
			</div>
			{posts.data && (
				<Pager
					page={posts.data.page}
					pageSize={posts.data.pageSize}
					total={posts.data.total}
					onPage={setPage}
				/>
			)}
		</div>
	);
}
