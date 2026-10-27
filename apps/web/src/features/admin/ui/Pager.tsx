import { AdminButton } from "./AdminButton";
import styles from "./admin.module.css";

interface PagerProps {
	page: number;
	pageSize: number;
	total: number;
	onPage: (page: number) => void;
}

export function Pager({ page, pageSize, total, onPage }: PagerProps) {
	const pages = Math.max(1, Math.ceil(total / pageSize));
	if (pages === 1) return null;
	return (
		<nav className={styles.pager} aria-label="Paginação">
			<AdminButton disabled={page <= 1} onClick={() => onPage(page - 1)}>
				Anterior
			</AdminButton>
			<span>
				Página {page} de {pages}
			</span>
			<AdminButton disabled={page >= pages} onClick={() => onPage(page + 1)}>
				Próxima
			</AdminButton>
		</nav>
	);
}
