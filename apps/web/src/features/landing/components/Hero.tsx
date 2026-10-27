import { Button } from "#/components/ui/Button";
import { Clover } from "#/components/ui/Clover";
import { HandNote } from "#/components/ui/HandNote";
import { Photo } from "#/components/ui/Photo";
import { Placeholder } from "#/components/ui/Placeholder";
import { formatDualTime, formatWeekdayShort } from "#/lib/time";
import type { LandingContent } from "../types";
import styles from "./Hero.module.css";

interface HeroProps {
	photo: LandingContent["hero"];
	nextMatch: LandingContent["nextMatch"];
}

export function Hero({ photo, nextMatch }: HeroProps) {
	const kickoff = nextMatch && new Date(nextMatch.startsAt);

	return (
		<section className={styles.hero} aria-labelledby="hero-title">
			<div className={styles.intro}>
				<span className={styles.eyebrow}>
					<Clover filled size={14} className={styles.eyebrowClover} />
					Torcida atleticana em Dublin, Cork e Galway
				</span>
				<h1 id="hero-title" className={styles.title}>
					O Galo nunca joga sozinho.
				</h1>
				<HandNote underline="clover">nem às 2 da manhã em Dublin.</HandNote>
				<p className={styles.lead}>
					Somos a turma que acorda de madrugada, atravessa a cidade no frio e
					enche um pub irlandês de mineiro. Jogo, churrasco, pelada e uma
					saudade danada de BH.
				</p>
			</div>

			<div className={styles.media}>
				{photo ? (
					<Photo
						photo={photo}
						sizes="(min-width: 960px) 560px, 100vw"
						priority
						className={styles.photo}
					/>
				) : (
					<Placeholder
						caption="[foto real: pub lotado na hora do gol]"
						className={styles.photo}
					/>
				)}
				{nextMatch && kickoff && (
					<div className={styles.nextCard}>
						<span className={styles.nextLabel}>
							<Clover filled size={12} className={styles.nextClover} />
							PRÓXIMO EMBARQUE
						</span>
						<span className={styles.nextTitle}>{nextMatch.title}</span>
						<span className={styles.nextTime}>
							{formatWeekdayShort(kickoff, "dublin")} ·{" "}
							{formatDualTime(kickoff)}
						</span>
					</div>
				)}
				<div className={styles.stamp} aria-hidden="true">
					<span className={styles.stampRoute}>DUB ⇄ CNF</span>
					<span className={styles.stampKm}>9.000</span>
					<span className={styles.stampText}>km de saudade</span>
				</div>
			</div>

			<div className={styles.actions}>
				<Button href="#partidas" arrow className={styles.primary}>
					Ver o próximo jogo
				</Button>
				<Button href="/seja-socio" variant="link">
					Quero ser sócio
				</Button>
			</div>
		</section>
	);
}
