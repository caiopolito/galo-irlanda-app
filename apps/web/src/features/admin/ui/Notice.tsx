import type { ReactNode } from "react";
import { cx } from "#/lib/cx";
import styles from "./admin.module.css";

export function Notice({
	tone = "info",
	children,
}: {
	tone?: "info" | "error" | "success";
	children: ReactNode;
}) {
	const toneClass = {
		info: styles.noticeInfo,
		error: styles.noticeError,
		success: styles.noticeSuccess,
	}[tone];
	return (
		<div
			className={cx(styles.notice, toneClass)}
			role={tone === "error" ? "alert" : "status"}
		>
			{children}
		</div>
	);
}
