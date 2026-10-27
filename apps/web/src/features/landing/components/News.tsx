import { Placeholder } from "#/components/ui/Placeholder";
import { SectionHeader } from "#/components/ui/SectionHeader";
import { links } from "#/config/links";
import { news } from "../data";
import styles from "./News.module.css";

export function News() {
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
						<li key={item.id} className={styles.item}>
							<Placeholder className={styles.image} />
							<div className={styles.text}>
								<span className={styles.meta}>
									{item.category} · {item.date}
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
