import type { ReactNode } from "react";
import { cx } from "#/lib/cx";
import styles from "./SectionHeader.module.css";

interface SectionHeaderProps {
	title: ReactNode;
	subtitle?: ReactNode;
	/** Conteúdo à direita do título (link, relógios, anotação). */
	aside?: ReactNode;
	id?: string;
	className?: string;
}

export function SectionHeader({
	title,
	subtitle,
	aside,
	id,
	className,
}: SectionHeaderProps) {
	return (
		<div className={cx(styles.header, className)}>
			<div className={styles.text}>
				<h2 id={id} className={styles.title}>
					{title}
				</h2>
				{subtitle && <p className={styles.subtitle}>{subtitle}</p>}
			</div>
			{aside}
		</div>
	);
}
