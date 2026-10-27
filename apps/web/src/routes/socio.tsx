import {
	createFileRoute,
	Link,
	Navigate,
	Outlet,
} from "@tanstack/react-router";
import { Logo } from "#/components/ui/Logo";
import { useMe } from "#/features/admin/hooks/useMe";
import { Notice } from "#/features/admin/ui/Notice";
import styles from "#/features/member/member.module.css";

export const Route = createFileRoute("/socio")({
	// Área do sócio roda só no navegador: depende do cookie de sessão.
	ssr: false,
	head: () => ({
		meta: [
			{ title: "Área do sócio · Galo Irlanda" },
			{ name: "robots", content: "noindex" },
		],
	}),
	component: MemberShell,
});

const TOP_NAV = [
	{ href: "/socio", label: "Painel" },
	{ href: "/socio#jogos", label: "Meus jogos" },
	{ href: "/socio#pagamentos", label: "Pagamentos" },
	{ href: "/socio#beneficios", label: "Benefícios" },
	{ href: "/socio#votacao", label: "Votações" },
];

function Icon({ d }: { d: string }) {
	return (
		<svg
			width="22"
			height="22"
			viewBox="0 0 24 24"
			fill="none"
			stroke="currentColor"
			strokeWidth="1.8"
			aria-hidden="true"
		>
			<path d={d} />
		</svg>
	);
}

function MemberShell() {
	const me = useMe();

	if (me.isPending) return <p style={{ padding: 32 }}>Carregando…</p>;
	if (me.data === null) return <Navigate to="/entrar" />;
	if (me.isError)
		return <Notice tone="error">Não foi possível falar com a API.</Notice>;

	if (!me.data.roles.includes("socio")) {
		const staff =
			me.data.roles.includes("admin") || me.data.roles.includes("editor");
		return (
			<main className={styles.main}>
				<Notice>
					Sua conta ainda não é de sócio.{" "}
					<Link to="/seja-socio">Seja sócio</Link>
					{staff && (
						<>
							{" "}
							ou volte para o <Link to="/admin">admin</Link>
						</>
					)}
					.
				</Notice>
			</main>
		);
	}

	const initial = me.data.displayName.charAt(0).toUpperCase();

	return (
		<div className={styles.shell}>
			<header className={styles.header}>
				<div className={styles.headerInner}>
					<a href="/" className={styles.brand}>
						<Logo size={44} />
						<span className={styles.back}>← site</span>
					</a>
					<nav aria-label="Área do sócio" className={styles.topNav}>
						{TOP_NAV.map((item) => (
							<a key={item.href} href={item.href} className={styles.topNavLink}>
								{item.label}
							</a>
						))}
					</nav>
					<Link
						to="/socio/perfil"
						className={styles.avatar}
						aria-label="Seu perfil"
					>
						{initial}
					</Link>
				</div>
			</header>
			<Outlet />
			<nav aria-label="Área do sócio" className={styles.bottomNav}>
				<a
					href="/socio#carteirinha"
					className={`${styles.bottomNavLink} ${styles.bottomNavLinkActive}`}
				>
					<Icon d="M5 6h14a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2zM14 6v13" />
					Carteirinha
				</a>
				<a href="/socio#jogos" className={styles.bottomNavLink}>
					<Icon d="M6 5h12a3 3 0 0 1 3 3v10a3 3 0 0 1-3 3H6a3 3 0 0 1-3-3V8a3 3 0 0 1 3-3zM3 10h18M8 3v4M16 3v4" />
					Jogos
				</a>
				<a href="/socio#votacao" className={styles.bottomNavLink}>
					<Icon d="M5 20V10M12 20V4M19 20v-7" />
					Votações
				</a>
				<Link
					to="/socio/perfil"
					className={styles.bottomNavLink}
					activeProps={{
						className: `${styles.bottomNavLink} ${styles.bottomNavLinkActive}`,
					}}
				>
					<Icon d="M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM4 21c1.5-4 4.5-6 8-6s6.5 2 8 6" />
					Perfil
				</Link>
			</nav>
		</div>
	);
}
