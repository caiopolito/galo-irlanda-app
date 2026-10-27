import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
	createFileRoute,
	Link,
	Navigate,
	Outlet,
	useNavigate,
} from "@tanstack/react-router";
import { useState } from "react";
import { Logo } from "#/components/ui/Logo";
import { useMe } from "#/features/admin/hooks/useMe";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import shell from "#/features/admin/ui/AdminShell.module.css";
import { Notice } from "#/features/admin/ui/Notice";
import { api, unwrap } from "#/lib/api";

export const Route = createFileRoute("/admin")({
	// O admin roda só no navegador: depende do cookie de sessão e não precisa de SEO.
	ssr: false,
	head: () => ({
		meta: [
			{ title: "Admin · Galo Irlanda" },
			{ name: "robots", content: "noindex" },
		],
	}),
	component: AdminLayout,
});

const NAV = [
	{ to: "/admin/eventos", label: "Eventos" },
	{ to: "/admin/noticias", label: "Notícias" },
	{ to: "/admin/fotos", label: "Fotos" },
	{ to: "/admin/cidades", label: "Cidades" },
	{ to: "/admin/locais", label: "Locais" },
	{ to: "/admin/marcos", label: "Marcos" },
	{ to: "/admin/socios", label: "Sócios" },
	{ to: "/admin/votacoes", label: "Votações" },
	{ to: "/admin/beneficios", label: "Benefícios" },
	{ to: "/admin/documentos", label: "Documentos" },
	{ to: "/admin/configuracoes", label: "Configurações" },
] as const;

const ADMIN_NAV = [
	{ to: "/admin/usuarios", label: "Usuários" },
	{ to: "/admin/historico", label: "Histórico" },
] as const;

function AdminLayout() {
	const me = useMe();
	const navigate = useNavigate();
	const queryClient = useQueryClient();
	const [published, setPublished] = useState(false);

	const logout = useMutation({
		mutationFn: () => unwrap(api.POST("/api/auth/logout")),
		onSuccess: async () => {
			queryClient.clear();
			await navigate({ to: "/entrar" });
		},
	});
	const rebuild = useMutation({
		mutationFn: () => unwrap(api.POST("/api/admin/publishing/rebuild")),
		onSuccess: () => setPublished(true),
	});

	if (me.isPending) return <p style={{ padding: 32 }}>Carregando…</p>;
	if (me.data === null) return <Navigate to="/entrar" />;
	if (me.isError)
		return (
			<Notice tone="error">
				Não foi possível falar com a API. Ela está rodando?
			</Notice>
		);

	const user = me.data;
	const isAdmin = user.roles.includes("admin");
	const canEdit = isAdmin || user.roles.includes("editor");
	if (!canEdit)
		return <Notice tone="error">Sua conta não tem acesso ao admin.</Notice>;

	return (
		<div className={shell.shell}>
			<aside className={shell.sidebar}>
				<Link to="/" className={shell.brand}>
					<Logo size={32} decorative />
					Galo Irlanda
				</Link>
				<nav className={shell.nav} aria-label="Admin">
					{[...NAV, ...(isAdmin ? ADMIN_NAV : [])].map((item) => (
						<Link
							key={item.to}
							to={item.to}
							className={shell.navLink}
							activeProps={{
								className: `${shell.navLink} ${shell.navLinkActive}`,
							}}
						>
							{item.label}
						</Link>
					))}
				</nav>
				<AdminButton
					variant="primary"
					busy={rebuild.isPending}
					onClick={() => rebuild.mutate()}
				>
					Publicar site agora
				</AdminButton>
				{published && (
					<span className={shell.user}>Site sendo atualizado (1–2 min).</span>
				)}
				<div className={shell.user}>
					<span>{user.displayName}</span>
					<AdminButton variant="ghost" onClick={() => logout.mutate()}>
						Sair
					</AdminButton>
				</div>
			</aside>
			<main className={shell.main}>
				<Outlet />
			</main>
		</div>
	);
}
