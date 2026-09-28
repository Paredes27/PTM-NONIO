# Validação Funcional

## Objetivo

Este documento resume os testes funcionais usados para confirmar que a versão final da aplicação está pronta para demonstração e avaliação.

## Comandos

> Executar a partir da raiz da solução (pasta que contém `MentoriaAPI.sln`).

Compilar a solução:

```bash
dotnet build MentoriaAPI.sln
```

Executar o validador funcional:

```bash
dotnet run --project MentoriaAPI.Validation
```

## Cenários Validados

- Seed inicial com utilizadores, roles, alunos, mentores, matches, sessões e feedback.
- Credencial de demonstração do aluno João.
- Geração de recomendações de matching para aluno existente.
- Prevenção de duplicação de match ativo para o mesmo aluno.
- Criação de sessão associada a emparelhamento ativo.
- Registo de feedback persistido numa sessão concluída.

## Regras Corrigidas na Versão Final

- O sistema já não cria matches ativos duplicados para o mesmo aluno.
- Mentores sem capacidade disponível deixam de ser recomendados.
- Sessões só podem ser criadas para emparelhamentos ativos.
- Sessões novas devem ser agendadas para data futura.
- Estados de sessão válidos: `Scheduled`, `Completed` e `Cancelled`.
- Feedback só é aceite quando a sessão está concluída.
- Autor e destinatário do feedback têm de pertencer ao emparelhamento da sessão.
- Autor e destinatário do feedback têm de ser utilizadores diferentes.
