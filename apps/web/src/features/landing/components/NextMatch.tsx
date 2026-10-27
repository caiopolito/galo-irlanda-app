import { Button } from "#/components/ui/Button";
import { Crest } from "#/components/ui/Crest";
import { Photo } from "#/components/ui/Photo";
import { Placeholder } from "#/components/ui/Placeholder";
import { formatDayMonth, formatDualTime, formatWeekdayLong } from "#/lib/time";
import type { LandingContent } from "../types";
import styles from "./NextMatch.module.css";

export function NextMatch({
	match,
}: {
	match: NonNullable<LandingContent["nextMatch"]>;
}) {
	const kickoff = new Date(match.startsAt);
	const details = [
		{
			term: "Quando",
			value: `${formatWeekdayLong(kickoff, "dublin")}, ${formatDayMonth(kickoff, "dublin")}`,
		},
		{ term: "Bola rola", value: formatDualTime(kickoff) },
		{ term: "Onde", value: match.place },
		...(match.homeTrip
			? [{ term: "Volta pra casa", value: match.homeTrip }]
			: []),
	];

	return (
		<section className={styles.section} aria-labelledby="proximo-jogo-title">
			<div className={styles.inner}>
				<span className={styles.label}>Portão 01 · o próximo jogo</span>
				<h2 id="proximo-jogo-title" className={styles.title}>
					<Crest height={56} className={styles.crest} />
					{match.title}
				</h2>
				{match.photo ? (
					<Photo
						photo={match.photo}
						sizes="(min-width: 960px) 560px, 100vw"
						className={styles.photo}
					/>
				) : (
					<Placeholder
						tone="light"
						caption="[foto: fachada do pub parceiro, à noite]"
						className={styles.photo}
					/>
				)}
				<dl className={styles.details}>
					{details.map((detail) => (
						<div key={detail.term}>
							<dt>{detail.term}</dt>
							<dd>{detail.value}</dd>
						</div>
					))}
				</dl>
				<div className={styles.actions}>
					<Button href="#to-dentro" variant="dark" className={styles.action}>
						Tô dentro
					</Button>
					<Button
						href="#por-na-agenda"
						variant="outlineDark"
						className={styles.action}
					>
						Pôr na agenda
					</Button>
					{match.confirmed > 0 && (
						<span className={styles.count}>
							{match.confirmed} atleticanos confirmados
						</span>
					)}
				</div>
			</div>
		</section>
	);
}
