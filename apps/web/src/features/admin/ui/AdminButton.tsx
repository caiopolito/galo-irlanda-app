import type { ButtonHTMLAttributes } from "react";
import { cx } from "#/lib/cx";
import styles from "./admin.module.css";

interface AdminButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
	variant?: "primary" | "secondary" | "danger" | "ghost";
	selected?: boolean;
	busy?: boolean;
}

export function AdminButton({
	variant = "secondary",
	selected = false,
	busy = false,
	className,
	children,
	disabled,
	type = "button",
	...rest
}: AdminButtonProps) {
	return (
		<button
			type={type}
			className={cx(
				styles.button,
				styles[variant],
				selected && styles.selected,
				className,
			)}
			disabled={disabled || busy}
			aria-busy={busy || undefined}
			{...rest}
		>
			{busy ? "Salvando…" : children}
		</button>
	);
}
