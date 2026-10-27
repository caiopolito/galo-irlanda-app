import type { ReactNode } from "react";
import styles from "./admin.module.css";

export function PageHeader({
	title,
	subtitle,
	actions,
}: {
	title: string;
	subtitle?: string;
	actions?: ReactNode;
}) {
	return (
		<header className={styles.pageHeader}>
			<div>
				<h1 className={styles.pageTitle}>{title}</h1>
				{subtitle && <p className={styles.pageSubtitle}>{subtitle}</p>}
			</div>
			{actions && <div className={styles.actions}>{actions}</div>}
		</header>
	);
}
