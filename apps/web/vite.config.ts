import { cloudflare } from "@cloudflare/vite-plugin";
import { tanstackStart } from "@tanstack/react-start/plugin/vite";
import viteReact from "@vitejs/plugin-react";
import { defineConfig } from "vite";
import { imagetools } from "vite-imagetools";

const config = defineConfig({
	resolve: { tsconfigPaths: true },
	plugins: [
		cloudflare({ viteEnvironment: { name: "ssr" } }),
		imagetools({
			defaultDirectives: (url) =>
				url.searchParams.has("photo")
					? new URLSearchParams({
							w: "480;960;1440",
							format: "avif;webp;jpg",
							as: "picture",
						})
					: new URLSearchParams(),
		}),
		tanstackStart(),
		viteReact(),
	],
});

export default config;
