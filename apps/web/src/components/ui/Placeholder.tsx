import { cx } from "#/lib/cx";
import styles from "./Placeholder.module.css";

interface PlaceholderProps {
	caption?: string;
	tone?: "dark" | "light";
	className?: string;
}

/** Área listrada no lugar de uma foto que ainda não existe, com a legenda do que deve ir ali. */
export function Placeholder({
	caption,
	tone = "dark",
	className,
}: PlaceholderProps) {
	return (
		<div className={cx(styles.placeholder, styles[tone], className)}>
			{caption && <span className={styles.caption}>{caption}</span>}
		</div>
	);
}
