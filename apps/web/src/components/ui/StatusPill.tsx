import type { ReactNode } from "react";
import { cx } from "#/lib/cx";
import { Clover } from "./Clover";
import styles from "./StatusPill.module.css";

export type StatusTone = "boarding" | "confirmed" | "muted" | "pending";

interface StatusPillProps {
	tone: StatusTone;
	children: ReactNode;
	size?: "sm" | "md";
	/** Trevo antes do texto (selos de pertencimento, como "Confirmado"). */
	clover?: boolean;
	className?: string;
}

/** Âmbar cheio = embarcando · contorno claro = confirmado · apagado = vagas · tracejado = em votação. */
export function StatusPill({
	tone,
	children,
	size = "md",
	clover = false,
	className,
}: StatusPillProps) {
	return (
		<span className={cx(styles.pill, styles[tone], styles[size], className)}>
			{clover && (
				<Clover
					filled
					size={size === "sm" ? 11 : 13}
					className={styles.clover}
				/>
			)}
			{children}
		</span>
	);
}
