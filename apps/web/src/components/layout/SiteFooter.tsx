import { Monogram } from "#/components/ui/Monogram";
import { links } from "#/config/links";
import styles from "./SiteFooter.module.css";

const FOOTER_LINKS = [
	{ href: links.instagram, label: "Instagram" },
	{ href: links.whatsappGroup, label: "WhatsApp" },
	{ href: links.partnerPubs, label: "Pubs parceiros" },
	{ href: links.contact, label: "Contato" },
];

export function SiteFooter() {
	return (
		<footer className={styles.footer}>
			<div className={styles.inner}>
				<p className={styles.tagline}>
					<Monogram size={40} />
					Eu acredito. Em qualquer fuso.
				</p>
				<nav aria-label="Rodapé" className={styles.nav}>
					{FOOTER_LINKS.map((link) => (
						<a key={link.label} href={link.href} className={styles.link}>
							{link.label}
						</a>
					))}
				</nav>
				<p className={styles.legal}>
					Torcida independente de atleticanos na Irlanda. Sem vínculo oficial
					com o Clube Atlético Mineiro.
				</p>
			</div>
		</footer>
	);
}
