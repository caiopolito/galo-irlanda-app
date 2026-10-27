import { useQuery } from "@tanstack/react-query";
import { Monogram } from "#/components/ui/Monogram";
import { api, apiBaseUrl, unwrap } from "#/lib/api";
import styles from "../member.module.css";
import { memberKeys } from "../useMember";

/** Carteirinha em formato de cartão de embarque, com QR code de verificação. */
export function BoardingCard() {
	const card = useQuery({
		queryKey: memberKeys.card,
		queryFn: () => unwrap(api.GET("/api/socio/card")),
	});
	if (!card.data)
		return <div className={styles.card} style={{ minHeight: 260 }} />;

	const { fullName, memberNumber, validUntil, isActive, qrSvg } = card.data;
	const valid = validUntil
		? new Date(validUntil).toLocaleDateString("pt-BR", {
				timeZone: "Europe/Dublin",
			})
		: "—";

	return (
		<div
			id="carteirinha"
			style={{ display: "flex", flexDirection: "column", gap: 12 }}
		>
			<div className={styles.card}>
				<div className={styles.cardMain}>
					<div className={styles.cardTop}>
						<span className={styles.cardTitle}>
							<Monogram size={30} />
							Cartão de embarque
						</span>
						<span className={styles.cardSeason}>
							TEMPORADA {new Date().getFullYear()}
						</span>
					</div>
					<div className={styles.route}>
						<span>DUB</span>
						<svg
							width="56"
							height="20"
							viewBox="0 0 56 20"
							fill="none"
							aria-hidden="true"
						>
							<path
								d="M2 14c14-10 34-10 50-2"
								stroke="currentColor"
								strokeWidth="2"
								strokeLinecap="round"
								strokeDasharray="3 5"
							/>
							<path
								d="M46 7l6 5-7 3"
								stroke="currentColor"
								strokeWidth="2"
								strokeLinecap="round"
								strokeLinejoin="round"
							/>
						</svg>
						<span>CNF</span>
					</div>
					<dl className={styles.fields}>
						<div>
							<dt>Passageiro</dt>
							<dd>{fullName}</dd>
						</div>
						<div>
							<dt>Nº</dt>
							<dd>{String(memberNumber).padStart(4, "0")}</dd>
						</div>
						<div>
							<dt>Válido até</dt>
							<dd>{isActive ? valid : "Pendente"}</dd>
						</div>
					</dl>
				</div>
				<div className={styles.cardStub}>
					{/* Como imagem (data URL), o SVG não entra no DOM da página. */}
					<img
						className={styles.qr}
						src={`data:image/svg+xml;charset=utf-8,${encodeURIComponent(qrSvg)}`}
						alt="QR code da carteirinha"
					/>
					<span>Mostre no pub parceiro pra ganhar o desconto de sócio.</span>
				</div>
			</div>
			<div className={styles.actions}>
				<button
					type="button"
					className={`${styles.btn} ${styles.btnLight}`}
					disabled
					title="Em breve"
				>
					Adicionar à Wallet (em breve)
				</button>
				<a href={`${apiBaseUrl}/api/socio/card.pdf`} className={styles.btn}>
					Baixar PDF
				</a>
			</div>
		</div>
	);
}
