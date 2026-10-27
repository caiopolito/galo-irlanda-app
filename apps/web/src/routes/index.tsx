import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/")({ component: Home });

function Home() {
	return (
		<main
			style={{
				maxWidth: "var(--container-max)",
				margin: "0 auto",
				padding: "64px var(--gutter)",
			}}
		>
			<h1 style={{ fontFamily: "var(--font-display)", fontWeight: 400 }}>
				O Galo nunca joga sozinho.
			</h1>
			<p
				style={{
					fontFamily: "var(--font-hand)",
					fontSize: 32,
					color: "var(--color-amber)",
				}}
			>
				nem às 2 da manhã em Dublin.
			</p>
		</main>
	);
}
