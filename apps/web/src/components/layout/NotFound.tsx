import { Link } from "@tanstack/react-router";
import { HandNote } from "#/components/ui/HandNote";

export function NotFound() {
	return (
		<main
			style={{
				maxWidth: "var(--container-max)",
				margin: "0 auto",
				padding: "96px var(--gutter)",
			}}
		>
			<h1
				style={{
					fontFamily: "var(--font-display)",
					fontWeight: 400,
					fontSize: "var(--text-h2)",
					margin: 0,
				}}
			>
				Portão não encontrado.
			</h1>
			<HandNote>esse voo não existe</HandNote>
			<p>
				<Link to="/" style={{ color: "var(--color-amber)" }}>
					Voltar para a página inicial
				</Link>
			</p>
		</main>
	);
}
