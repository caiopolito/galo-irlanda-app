import { Monogram } from "#/components/ui/Monogram";
import { type ExternalLinks, externalLinks, links } from "#/config/links";
import styles from "./SiteFooter.module.css";

export function SiteFooter({
	external = externalLinks(),
}: {
	external?: ExternalLinks;
}) {
	const footerLinks = [
		{ href: external.instagram, label: "Instagram" },
		{ href: external.whatsappGroup, label: "WhatsApp" },
		{ href: links.partnerPubs, label: "Pubs parceiros" },
		{ href: external.contact, label: "Contato" },
	];

	return (
		<footer className={styles.footer}>
			<div className={styles.inner}>
				<p className={styles.tagline}>
					<Monogram size={40} />
					Eu acredito. Em qualquer fuso.
				</p>
				<nav aria-label="Rodapé" className={styles.nav}>
					{footerLinks.map((link) => (
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
