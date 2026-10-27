import { useMutation, useQuery } from "@tanstack/react-query";
import { api, unwrap } from "#/lib/api";
import { formatMoney } from "#/lib/money";
import styles from "../member.module.css";
import { memberKeys } from "../useMember";

const STATUS: Record<string, string> = {
	past_due:
		"Pagamento atrasado: atualize o cartão para não perder os benefícios.",
	unpaid: "Mensalidade em aberto.",
	canceled: "Assinatura cancelada.",
	incomplete: "Pagamento não concluído.",
	none: "Sem assinatura ativa.",
};

function month(iso: string) {
	const label = new Intl.DateTimeFormat("pt-BR", {
		month: "long",
		year: "numeric",
		timeZone: "Europe/Dublin",
	}).format(new Date(iso));
	return label.charAt(0).toUpperCase() + label.slice(1);
}

export function Billing() {
	const billing = useQuery({
		queryKey: memberKeys.billing,
		queryFn: () => unwrap(api.GET("/api/socio/billing")),
	});
	const portal = useMutation({
		mutationFn: () => unwrap(api.POST("/api/socio/billing/portal")),
		onSuccess: ({ url }) => window.location.assign(url),
	});
	const data = billing.data;
	const membership = data?.membership;

	return (
		<section id="pagamentos" className={styles.panel}>
			<div className={styles.sectionHead}>
				<h2 className={styles.h2}>Mensalidade</h2>
				{data?.feeCents != null && (
					<span className={styles.amount}>
						{formatMoney(data.feeCents, data.currency)}
						<small>/mês</small>
					</span>
				)}
			</div>
			{membership && !membership.isActive && (
				<p className={styles.text}>
					{STATUS[membership.status] ?? membership.status}
				</p>
			)}
			{membership?.isActive && membership.currentPeriodEnd && (
				<p className={styles.text}>
					{membership.cancelAtPeriodEnd
						? "Assinatura termina em "
						: "Próxima cobrança em "}
					{new Date(membership.currentPeriodEnd).toLocaleDateString("pt-BR", {
						timeZone: "Europe/Dublin",
					})}
					{data?.cardLast4 && ` no cartão final ${data.cardLast4}`}.
				</p>
			)}
			{data && data.invoices.length > 0 && (
				<ul className={styles.invoices}>
					{data.invoices.map((invoice) => (
						<li key={invoice.date}>
							<span>{month(invoice.date)}</span>
							<span className={styles.muted}>
								{formatMoney(invoice.amountCents, invoice.currency)}
								{invoice.receiptUrl && (
									<>
										{" · "}
										<a
											href={invoice.receiptUrl}
											target="_blank"
											rel="noreferrer"
											style={{ color: "var(--color-text)" }}
										>
											recibo
										</a>
									</>
								)}
							</span>
						</li>
					))}
				</ul>
			)}
			<div className={styles.actions}>
				<button
					type="button"
					className={styles.btn}
					disabled={portal.isPending}
					onClick={() => portal.mutate()}
				>
					{membership?.isActive
						? "Gerenciar pagamento"
						: "Regularizar pagamento"}
				</button>
			</div>
			{portal.isError && (
				<span className={styles.muted}>
					Não foi possível abrir o portal de pagamento agora.
				</span>
			)}
		</section>
	);
}
