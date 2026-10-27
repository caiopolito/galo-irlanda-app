import { Photo } from "#/components/ui/Photo";
import { Placeholder } from "#/components/ui/Placeholder";
import { SectionHeader } from "#/components/ui/SectionHeader";
import type { LandingContent } from "../types";
import styles from "./Cities.module.css";

export function Cities({ cities }: { cities: LandingContent["cities"] }) {
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
							{city.photo ? (
								<Photo
									photo={city.photo}
									sizes="(min-width: 720px) 400px, 260px"
									className={styles.placeholder}
								/>
							) : (
								<Placeholder
									caption={`[foto: a turma de ${city.name}]`}
									className={styles.placeholder}
								/>
							)}
							<span className={styles.code}>{city.code.trim()}</span>
						</div>
						<div className={styles.body}>
							<h3 className={styles.name}>{city.name}</h3>
							{city.blurb && <p className={styles.blurb}>{city.blurb}</p>}
						</div>
					</li>
				))}
			</ul>
		</section>
	);
}
