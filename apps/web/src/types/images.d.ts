// Fotos importadas com `?photo` viram <picture> responsivo (ver vite.config.ts).
declare module "*?photo" {
	import type { Picture } from "vite-imagetools";
	const picture: Picture;
	export default picture;
}
