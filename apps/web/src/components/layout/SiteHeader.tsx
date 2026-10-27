import { useState } from "react";
import { Button } from "#/components/ui/Button";
import { Clover } from "#/components/ui/Clover";
import { Logo } from "#/components/ui/Logo";
import { type ExternalLinks, externalLinks, links } from "#/config/links";
import { cx } from "#/lib/cx";
import styles from "./SiteHeader.module.css";

const NAV_ITEMS = [
	{ href: "#partidas", label: "Jogos" },
	{ href: "#torcida", label: "Da Torcida" },
	{ href: "#fotos", label: "Fotos" },
	{ href: "#cidades", label: "Pubs" },
	{ href: links.memberArea, label: "Área do sócio" },
];

export function SiteHeader({
	external = externalLinks(),
}: {
	external?: ExternalLinks;
}) {
	const [menuOpen, setMenuOpen] = useState(false);

	return (
		<header className={styles.header}>
			<div className={styles.inner}>
				<a href="/" className={styles.brand}>
					<Logo size={48} decorative className={styles.logo} />
					<span className={styles.wordmark}>
						Galo <Clover filled size={15} className={styles.wordmarkClover} />{" "}
						Irlanda
					</span>
				</a>

				<nav
					id="menu-principal"
					aria-label="Principal"
					className={cx(styles.nav, menuOpen && styles.navOpen)}
				>
					{NAV_ITEMS.map((item) => (
						<a
							key={item.href}
							href={item.href}
							className={styles.navLink}
							onClick={() => setMenuOpen(false)}
						>
							{item.label}
						</a>
					))}
				</nav>

				<Button
					href={external.whatsappGroup}
					variant="light"
					size="md"
					arrow
					className={styles.cta}
				>
					Entrar no grupo
				</Button>

				<div className={styles.mobileActions}>
					<a href={links.memberArea} className={styles.iconButton}>
						<svg
							width="20"
							height="20"
							viewBox="0 0 24 24"
							fill="none"
							stroke="currentColor"
							strokeWidth="1.8"
							aria-hidden="true"
						>
							<circle cx="12" cy="8" r="4" />
							<path d="M4 21c1.5-4 4.5-6 8-6s6.5 2 8 6" />
						</svg>
						<span className="sr-only">Área do sócio</span>
					</a>
					<button
						type="button"
						aria-label={menuOpen ? "Fechar menu" : "Abrir menu"}
						aria-expanded={menuOpen}
						aria-controls="menu-principal"
						className={cx(styles.iconButton, styles.iconButtonLight)}
						onClick={() => setMenuOpen((open) => !open)}
					>
						<svg
							width="20"
							height="20"
							viewBox="0 0 24 24"
							fill="none"
							stroke="currentColor"
							strokeWidth="2"
							strokeLinecap="round"
							aria-hidden="true"
						>
							{menuOpen ? (
								<path d="M6 6l12 12M18 6L6 18" />
							) : (
								<path d="M4 8h16M4 16h11" />
							)}
						</svg>
					</button>
				</div>
			</div>
		</header>
	);
}
