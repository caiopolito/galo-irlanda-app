import { Photo } from "#/components/ui/Photo";
import { Placeholder } from "#/components/ui/Placeholder";
import { SectionHeader } from "#/components/ui/SectionHeader";
import { links } from "#/config/links";
import { formatDayMonth } from "#/lib/time";
import type { LandingContent } from "../types";
import styles from "./News.module.css";

export function News({ news }: { news: LandingContent["news"] }) {
	if (news.length === 0) return null;

	return (
		<section
			id="torcida"
			className={styles.section}
			aria-labelledby="torcida-title"
		>
			<div className={styles.inner}>
				<SectionHeader
					id="torcida-title"
					title="Da Torcida"
					aside={
						<a href={links.allNews} className={styles.all}>
							Todas as notícias
						</a>
					}
				/>
				<ul className={styles.list}>
					{news.map((item) => (
						<li key={item.slug} className={styles.item}>
							{item.cover ? (
								<Photo
									photo={item.cover}
									sizes="(min-width: 720px) 400px, 100vw"
									className={styles.image}
								/>
							) : (
								<Placeholder className={styles.image} />
							)}
							<div className={styles.text}>
								<span className={styles.meta}>
									{item.category ? `${item.category} · ` : ""}
									{formatDayMonth(new Date(item.publishedAt), "dublin")}
								</span>
								<h3 className={styles.title}>{item.title}</h3>
							</div>
						</li>
					))}
				</ul>
			</div>
		</section>
	);
}
