import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createRootRoute, HeadContent, Scripts } from "@tanstack/react-router";
import { useState } from "react";

import appCss from "../styles.css?url";

export const Route = createRootRoute({
	head: () => ({
		meta: [
			{ charSet: "utf-8" },
			{ name: "viewport", content: "width=device-width, initial-scale=1" },
			{ name: "theme-color", content: "#121110" },
			{ title: "Galo Irlanda" },
			{
				name: "description",
				content:
					"Torcida de atleticanos na Irlanda. O Galo nunca joga sozinho.",
			},
		],
		links: [
			{ rel: "stylesheet", href: appCss },
			{ rel: "icon", type: "image/png", href: "/favicon.png" },
			{ rel: "apple-touch-icon", href: "/apple-touch-icon.png" },
		],
	}),
	shellComponent: RootDocument,
});

function RootDocument({ children }: { children: React.ReactNode }) {
	const [queryClient] = useState(
		() =>
			new QueryClient({
				defaultOptions: { queries: { staleTime: 30_000, retry: false } },
			}),
	);

	return (
		<html lang="pt-BR">
			<head>
				<HeadContent />
			</head>
			<body>
				<QueryClientProvider client={queryClient}>
					{children}
				</QueryClientProvider>
				<Scripts />
			</body>
		</html>
	);
}
