import type { ReactNode } from "react";
import { cx } from "#/lib/cx";
import { Clover } from "./Clover";
import styles from "./HandNote.module.css";

interface HandNoteProps {
	children: ReactNode;
	tone?: "amber" | "soft";
	/** `clover`: o sublinhado termina num trevo verde. */
	underline?: boolean | "clover";
	className?: string;
}

/** Anotação escrita à mão. No máximo uma por seção. */
export function HandNote({
	children,
	tone = "amber",
	underline = false,
	className,
}: HandNoteProps) {
	return (
		<span className={cx(styles.wrapper, className)}>
			<span className={cx(styles.note, styles[tone])}>{children}</span>
			{underline === "clover" && (
				<span className={styles.cloverUnderline} aria-hidden="true">
					<svg
						className={styles.underline}
						viewBox="0 0 120 16"
						fill="none"
						aria-hidden="true"
					>
						<path
							d="M2 10c18-6 34-7 52-3s40 4 64-4"
							stroke="currentColor"
							strokeWidth="2.5"
							strokeLinecap="round"
						/>
					</svg>
					<Clover size={30} className={styles.cloverEnd} />
				</span>
			)}
			{underline === true && (
				<svg
					className={styles.underline}
					viewBox="0 0 120 16"
					fill="none"
					aria-hidden="true"
				>
					<path
						d="M2 10c18-6 34-7 52-3s40 4 64-4"
						stroke="currentColor"
						strokeWidth="2.5"
						strokeLinecap="round"
					/>
				</svg>
			)}
		</span>
	);
}
