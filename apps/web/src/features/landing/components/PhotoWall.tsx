import { HandNote } from "#/components/ui/HandNote";
import { Photo } from "#/components/ui/Photo";
import { SectionHeader } from "#/components/ui/SectionHeader";
import { wallPhotos } from "../data";
import styles from "./PhotoWall.module.css";

export function PhotoWall() {
	return (
		<section
			id="fotos"
			className={styles.section}
			aria-labelledby="fotos-title"
		>
			<div className={styles.inner}>
				<SectionHeader
					id="fotos-title"
					title="Bagagem de mão"
					subtitle="As fotos de cada madrugada. Mande as suas no grupo."
					aside={<HandNote>foto tremida também vale</HandNote>}
				/>
				<ul className={styles.grid}>
					{wallPhotos.map((photo) => (
						<li key={photo.title}>
							<figure className={styles.figure}>
								<Photo
									picture={photo.picture}
									alt={photo.alt}
									sizes="(min-width: 960px) 280px, 50vw"
									className={styles.photo}
								/>
								<figcaption className={styles.caption}>
									<strong>{photo.title}</strong> {photo.caption}
								</figcaption>
							</figure>
						</li>
					))}
				</ul>
			</div>
		</section>
	);
}
