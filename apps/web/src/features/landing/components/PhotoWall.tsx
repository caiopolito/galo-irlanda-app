import { HandNote } from "#/components/ui/HandNote";
import { Photo } from "#/components/ui/Photo";
import { SectionHeader } from "#/components/ui/SectionHeader";
import type { LandingContent } from "../types";
import styles from "./PhotoWall.module.css";

export function PhotoWall({
	photos,
}: {
	photos: LandingContent["wallPhotos"];
}) {
	if (photos.length === 0) return null;

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
					{photos.map((item) => (
						<li key={item.photo.baseUrl}>
							<figure className={styles.figure}>
								<Photo
									photo={item.photo}
									sizes="(min-width: 960px) 280px, 50vw"
									className={styles.photo}
								/>
								{(item.title || item.caption) && (
									<figcaption className={styles.caption}>
										{item.title && <strong>{item.title}</strong>} {item.caption}
									</figcaption>
								)}
							</figure>
						</li>
					))}
				</ul>
			</div>
		</section>
	);
}
