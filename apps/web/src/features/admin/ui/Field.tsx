import { type ReactNode, useId } from "react";
import { cx } from "#/lib/cx";
import styles from "./admin.module.css";

interface FieldProps {
	label: string;
	hint?: ReactNode;
	error?: string;
	full?: boolean;
	/** Recebe o id do campo para ligar o <label>. */
	children: (id: string, invalid: boolean) => ReactNode;
}

export function Field({
	label,
	hint,
	error,
	full = false,
	children,
}: FieldProps) {
	const id = useId();
	return (
		<div className={cx(styles.field, full && styles.full)}>
			<label htmlFor={id} className={styles.label}>
				{label}
			</label>
			{children(id, Boolean(error))}
			{error ? (
				<span className={styles.error}>{error}</span>
			) : (
				hint && <span className={styles.hint}>{hint}</span>
			)}
		</div>
	);
}

export const inputClass = (invalid: boolean) =>
	cx(styles.input, invalid && styles.invalid);
export const selectClass = (invalid: boolean) =>
	cx(styles.select, invalid && styles.invalid);
export const textareaClass = (invalid: boolean) =>
	cx(styles.textarea, invalid && styles.invalid);
