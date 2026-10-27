import type { LandingContent } from "../types";
import styles from "./Timeline.module.css";

export function Timeline({
	milestones,
}: {
	milestones: LandingContent["milestones"];
}) {
	if (milestones.length === 0) return null;

	return (
		<section className={styles.section} aria-labelledby="escalas-title">
			<h2 id="escalas-title" className={styles.title}>
				Escalas de uma história que começou em 1908.
			</h2>
			<ol className={styles.list}>
				{milestones.map((milestone) => (
					<li
						key={`${milestone.year}-${milestone.text}`}
						className={styles.item}
					>
						<span className={styles.year}>{milestone.year}</span>
						<span className={styles.text}>{milestone.text}</span>
					</li>
				))}
			</ol>
		</section>
	);
}
