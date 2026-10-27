import { cx } from "#/lib/cx";
import styles from "./Monogram.module.css";

/** Monograma "GI", marca própria da torcida. */
export function Monogram({
	size = 44,
	className,
}: {
	size?: number;
	className?: string;
}) {
	return (
		<span
			className={cx(styles.monogram, className)}
			style={{ width: size, height: size, fontSize: size * 0.41 }}
			aria-hidden="true"
		>
			GI
		</span>
	);
}
