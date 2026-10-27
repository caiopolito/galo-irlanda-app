import { createFileRoute } from "@tanstack/react-router";
import { Logo } from "#/components/ui/Logo";
import styles from "#/features/member/signup.module.css";
import { api, unwrap } from "#/lib/api";

export const Route = createFileRoute("/carteirinha/$token")({
	loader: ({ params }) =>
		unwrap(
			api.GET("/api/public/card/{token}", {
				params: { path: { token: params.token } },
			}),
		),
	head: () => ({
		meta: [
			{ title: "Carteirinha · Galo Irlanda" },
			{ name: "robots", content: "noindex" },
		],
	}),
	component: VerifyCard,
});

/** O que o pub parceiro vê ao ler o QR da carteirinha. */
function VerifyCard() {
	const card = Route.useLoaderData();
	const validUntil = card.validUntil
		? new Date(card.validUntil).toLocaleDateString("pt-BR", {
				timeZone: "Europe/Dublin",
			})
		: null;

	return (
		<main className={styles.result}>
			<Logo size={72} />
			<div className={styles.verdict}>
				{!card.valid ? (
					<>
						<span className={styles.bad}>✗ Carteirinha inválida</span>
						<h1 className={styles.verdictTitle}>Este QR não é de um sócio.</h1>
						<p>
							Pode ser uma carteirinha antiga ou reemitida. Peça para a pessoa
							abrir a área do sócio.
						</p>
					</>
				) : (
					<>
						<span className={card.isActive ? styles.ok : styles.bad}>
							{card.isActive ? "✓ Sócio em dia" : "✗ Mensalidade pendente"}
						</span>
						<h1 className={styles.verdictTitle}>{card.fullName}</h1>
						<p>
							Sócio nº {String(card.memberNumber).padStart(4, "0")}
							{validUntil && card.isActive && ` · válido até ${validUntil}`}
						</p>
					</>
				)}
			</div>
			<p style={{ color: "var(--color-muted)", fontSize: 13 }}>
				Galo Irlanda · torcida de atleticanos na Irlanda
			</p>
		</main>
	);
}
