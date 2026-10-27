import { Button } from "#/components/ui/Button";
import { HandNote } from "#/components/ui/HandNote";
import { Photo } from "#/components/ui/Photo";
import { formatDualTime, formatWeekdayShort } from "#/lib/time";
import { hero, nextMatch } from "../data";
import styles from "./Hero.module.css";

export function Hero() {
	const kickoff = new Date(nextMatch.startsAt);

	return (
		<section className={styles.hero} aria-labelledby="hero-title">
			<div className={styles.intro}>
				<span className={styles.eyebrow}>
					<span className={styles.dot} aria-hidden="true" />
					Torcida atleticana em Dublin, Cork e Galway
				</span>
				<h1 id="hero-title" className={styles.title}>
					O Galo nunca joga sozinho.
				</h1>
				<HandNote underline>nem às 2 da manhã em Dublin.</HandNote>
				<p className={styles.lead}>
					Somos a turma que acorda de madrugada, atravessa a cidade no frio e
					enche um pub irlandês de mineiro. Jogo, churrasco, pelada e uma
					saudade danada de BH.
				</p>
			</div>

			<div className={styles.media}>
				<Photo
					picture={hero.photo}
					alt={hero.photoAlt}
					sizes="(min-width: 960px) 560px, 100vw"
					priority
					className={styles.photo}
				/>
				<div className={styles.nextCard}>
					<span className={styles.nextLabel}>PRÓXIMO EMBARQUE</span>
					<span className={styles.nextTitle}>{nextMatch.title}</span>
					<span className={styles.nextTime}>
						{formatWeekdayShort(kickoff, "dublin")} · {formatDualTime(kickoff)}
					</span>
				</div>
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
				<Button href="#socio" variant="link">
					Quero ser sócio
				</Button>
			</div>
		</section>
	);
}
