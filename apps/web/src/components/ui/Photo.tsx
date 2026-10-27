import type { Picture } from "vite-imagetools";
import { cx } from "#/lib/cx";
import styles from "./Photo.module.css";

interface PhotoProps {
	picture: Picture;
	alt: string;
	/** Atributo `sizes` do <img>: quanto da tela a foto ocupa. */
	sizes: string;
	priority?: boolean;
	className?: string;
}

export function Photo({
	picture,
	alt,
	sizes,
	priority = false,
	className,
}: PhotoProps) {
	return (
		<picture className={cx(styles.picture, className)}>
			{Object.entries(picture.sources).map(([format, srcSet]) => (
				<source
					key={format}
					type={`image/${format === "jpg" ? "jpeg" : format}`}
					srcSet={srcSet}
					sizes={sizes}
				/>
			))}
			<img
				src={picture.img.src}
				width={picture.img.w}
				height={picture.img.h}
				alt={alt}
				loading={priority ? "eager" : "lazy"}
				fetchPriority={priority ? "high" : "auto"}
				decoding="async"
				className={styles.img}
			/>
		</picture>
	);
}
