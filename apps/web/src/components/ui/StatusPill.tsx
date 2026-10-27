import type { ReactNode } from "react";
import { cx } from "#/lib/cx";
import styles from "./StatusPill.module.css";

export type StatusTone = "boarding" | "confirmed" | "muted" | "pending";

interface StatusPillProps {
	tone: StatusTone;
	children: ReactNode;
	size?: "sm" | "md";
	className?: string;
}

/** Âmbar cheio = embarcando · contorno claro = confirmado · apagado = vagas · tracejado = em votação. */
export function StatusPill({
	tone,
	children,
	size = "md",
	className,
}: StatusPillProps) {
	return (
		<span className={cx(styles.pill, styles[tone], styles[size], className)}>
			{children}
		</span>
	);
}
