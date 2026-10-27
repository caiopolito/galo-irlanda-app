import { createFileRoute, Link } from "@tanstack/react-router";
import { SiteFooter } from "#/components/layout/SiteFooter";
import { SiteHeader } from "#/components/layout/SiteHeader";
import { Button } from "#/components/ui/Button";
import { HandNote } from "#/components/ui/HandNote";
import styles from "#/features/member/signup.module.css";

export const Route = createFileRoute("/seja-socio/obrigado")({
	head: () => ({
		meta: [
			{ title: "Bem-vindo à Massa · Galo Irlanda" },
			{ name: "robots", content: "noindex" },
		],
	}),
	component: Thanks,
});

function Thanks() {
	return (
		<>
			<SiteHeader />
			<main className={styles.result}>
				<HandNote underline>embarque confirmado</HandNote>
				<h1 className={styles.title}>Bem-vindo à Massa!</h1>
				<p className={styles.lead}>
					Pagamento recebido. Em instantes chega no seu e-mail o número da sua
					carteirinha. Para entrar na área do sócio, use o mesmo e-mail do
					cadastro: mandamos um link de acesso, sem senha.
				</p>
				<Button href="/entrar" arrow>
					Entrar na área do sócio
				</Button>
				<Link to="/" style={{ color: "var(--color-text-soft)" }}>
					Voltar para o site
				</Link>
			</main>
			<SiteFooter />
		</>
	);
}
