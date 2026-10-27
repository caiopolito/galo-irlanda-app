import type { ReactNode } from "react";
import { cx } from "#/lib/cx";
import styles from "./Button.module.css";

type Variant = "amber" | "light" | "dark" | "outlineDark" | "link";

interface ButtonProps {
	href: string;
	children: ReactNode;
	variant?: Variant;
	size?: "md" | "lg";
	arrow?: boolean;
	className?: string;
}

export function Button({
	href,
	children,
	variant = "amber",
	size = "lg",
	arrow = false,
	className,
}: ButtonProps) {
	return (
		<a
			href={href}
			className={cx(styles.button, styles[variant], styles[size], className)}
		>
			{children}
			{arrow && <span aria-hidden="true">→</span>}
		</a>
	);
}
