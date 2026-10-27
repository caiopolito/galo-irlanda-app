import { cx } from "#/lib/cx";
import styles from "./Photo.module.css";

/** Foto servida pela API: variantes WebP em `{baseUrl}{480|1024|2048}.webp`. */
export interface PhotoSource {
	baseUrl: string;
	width: number;
	height: number;
	alt: string;
}

const VARIANT_WIDTHS = [480, 1024, 2048];

/** srcset com a largura real de cada variante (a API não amplia fotos menores que 2048 px). */
function srcSet({ baseUrl, width }: PhotoSource) {
	const seen = new Set<number>();
	return VARIANT_WIDTHS.flatMap((variant) => {
		const actual = Math.min(variant, width);
		if (seen.has(actual)) return [];
		seen.add(actual);
		return [`${baseUrl}${variant}.webp ${actual}w`];
	}).join(", ");
}

interface PhotoProps {
	photo: PhotoSource;
	/** Atributo `sizes` do <img>: quanto da tela a foto ocupa. */
	sizes: string;
	priority?: boolean;
	className?: string;
}

export function Photo({
	photo,
	sizes,
	priority = false,
	className,
}: PhotoProps) {
	return (
		<div className={cx(styles.picture, className)}>
			<img
				src={`${photo.baseUrl}1024.webp`}
				srcSet={srcSet(photo)}
				sizes={sizes}
				width={photo.width}
				height={photo.height}
				alt={photo.alt}
				loading={priority ? "eager" : "lazy"}
				fetchPriority={priority ? "high" : "auto"}
				decoding="async"
				className={styles.img}
			/>
		</div>
	);
}
