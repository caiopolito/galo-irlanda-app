import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { StatusPill, type StatusTone } from "#/components/ui/StatusPill";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import { inputClass } from "#/features/admin/ui/Field";
import { formatDateTime } from "#/features/admin/ui/labels";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { Pager } from "#/features/admin/ui/Pager";
import { api, unwrap } from "#/lib/api";

export const Route = createFileRoute("/admin/socios")({ component: Members });

const STATUS: Record<string, { label: string; tone: StatusTone }> = {
	active: { label: "Em dia", tone: "confirmed" },
	trialing: { label: "Em teste", tone: "confirmed" },
	past_due: { label: "Atrasado", tone: "boarding" },
	unpaid: { label: "Em aberto", tone: "boarding" },
	canceled: { label: "Cancelado", tone: "pending" },
	none: { label: "Sem assinatura", tone: "pending" },
};

function Members() {
	const queryClient = useQueryClient();
	const [search, setSearch] = useState("");
	const [page, setPage] = useState(1);
	const members = useQuery({
		queryKey: ["members", search, page],
		queryFn: () =>
			unwrap(
				api.GET("/api/admin/members", {
					params: { query: { search: search || undefined, Page: page } },
				}),
			),
	});
	const reissue = useMutation({
		mutationFn: (userId: string) =>
			unwrap(
				api.POST("/api/admin/members/{userId}/reissue-card", {
					params: { path: { userId } },
				}),
			),
		onSuccess: () => queryClient.invalidateQueries({ queryKey: ["members"] }),
	});

	return (
		<div className={styles.page}>
			<PageHeader
				title="Sócios"
				subtitle="Quem pagou virou sócio. A situação da mensalidade vem do Stripe."
			/>
			<input
				className={inputClass(false)}
				placeholder="Buscar por nome, e-mail ou número"
				value={search}
				onChange={(e) => {
					setSearch(e.target.value);
					setPage(1);
				}}
				style={{ maxWidth: 420 }}
			/>
			<div className={styles.tableWrap}>
				<table className={styles.table}>
					<thead>
						<tr>
							<th scope="col">Nº</th>
							<th scope="col">Sócio</th>
							<th scope="col">Cidade</th>
							<th scope="col">Mensalidade</th>
							<th scope="col">Desde</th>
							<th scope="col" />
						</tr>
					</thead>
					<tbody>
						{members.data?.items.map((member) => {
							const status = STATUS[member.status] ?? {
								label: member.status,
								tone: "muted" as const,
							};
							return (
								<tr key={member.userId}>
									<td>
										<strong>
											{String(member.memberNumber).padStart(4, "0")}
										</strong>
									</td>
									<td>
										{member.fullName}
										<div className={styles.muted}>{member.email}</div>
									</td>
									<td>{member.cityCode}</td>
									<td>
										<StatusPill tone={status.tone} size="sm">
											{status.label}
										</StatusPill>
										{member.paidUntil && (
											<div className={styles.muted}>
												até {formatDateTime(member.paidUntil).split(" ")[0]}
											</div>
										)}
									</td>
									<td className={styles.muted}>
										{formatDateTime(member.joinedAt).split(" ")[0]}
									</td>
									<td>
										<AdminButton
											variant="ghost"
											onClick={() =>
												confirm(
													`Reemitir a carteirinha de ${member.fullName}? O QR antigo deixa de valer.`,
												) && reissue.mutate(member.userId)
											}
										>
											Reemitir carteirinha
										</AdminButton>
									</td>
								</tr>
							);
						})}
					</tbody>
				</table>
				{members.data?.items.length === 0 && (
					<p className={styles.empty}>Nenhum sócio encontrado.</p>
				)}
			</div>
			{members.data && (
				<Pager
					page={members.data.page}
					pageSize={members.data.pageSize}
					total={members.data.total}
					onPage={setPage}
				/>
			)}
		</div>
	);
}
