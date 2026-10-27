import { Placeholder } from "#/components/ui/Placeholder";
import { SectionHeader } from "#/components/ui/SectionHeader";
import { CITY_NAMES, cities } from "../data";
import styles from "./Cities.module.css";

export function Cities() {
	return (
		<section
			id="cidades"
			className={styles.section}
			aria-labelledby="cidades-title"
		>
			<SectionHeader
				id="cidades-title"
				title="Três portões, uma Massa só."
				subtitle="Mora longe de Dublin? Tem atleticano perto de você também."
				className={styles.header}
			/>
			<p className={styles.hint} aria-hidden="true">
				Arraste pro lado →
			</p>
			<ul className={styles.list}>
				{cities.map((city) => (
					<li key={city.code} className={styles.card}>
						<div className={styles.media}>
							<Placeholder
								caption={city.photoCaption}
								className={styles.placeholder}
							/>
							<span className={styles.code}>{city.code}</span>
						</div>
						<div className={styles.body}>
							<h3 className={styles.name}>{CITY_NAMES[city.code]}</h3>
							<p className={styles.blurb}>{city.blurb}</p>
						</div>
					</li>
				))}
			</ul>
		</section>
	);
}
