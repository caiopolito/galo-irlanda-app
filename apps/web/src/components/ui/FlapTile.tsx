import type { ReactNode } from "react";
import { cx } from "#/lib/cx";
import styles from "./FlapTile.module.css";

interface FlapTileProps {
	children: ReactNode;
	size?: "sm" | "md";
	accent?: boolean;
	className?: string;
}

/** Plaquinha do painel de partidas (split-flap). */
export function FlapTile({
	children,
	size = "md",
	accent = false,
	className,
}: FlapTileProps) {
	return (
		<span
			className={cx(
				styles.tile,
				styles[size],
				accent && styles.accent,
				className,
			)}
		>
			{children}
		</span>
	);
}
