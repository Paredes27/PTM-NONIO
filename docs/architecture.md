# Arquitetura

## Visão geral

O sistema segue uma arquitetura monolítica em camadas:

- `Presentation`: frontend estático em `wwwroot`
- `API`: controllers ASP.NET Core REST
- `Application/Services`: autenticação JWT, matching e seed
- `Data`: Entity Framework Core com SQLite

## Componentes

### Frontend

- `index.html`: shell principal da aplicação
- `styles.css`: identidade visual e layout responsivo
- `app.js`: login, chamadas à API, renderização de dashboard

### Backend

- `AuthController`: login e registo
- `UsersController`, `StudentsController`, `MentorsController`, `RolesController`: gestão de perfis
- `MatchesController`: recomendações, criação de matches e métricas
- `SessionsController`: agendamento e estado de sessões
- `FeedbackController`: avaliações e comentários

### Persistência

Entidades principais:

- `User`
- `Role`
- `UserRole`
- `StudentProfile`
- `MentorProfile`
- `MentorshipMatch`
- `MentoringSession`
- `Feedback`

## Regras principais

- Cada `User` pode ser aluno, mentor ou administrador através de roles
- Cada aluno pode ter perfil académico
- Cada mentor pode ter perfil de especialização e capacidade máxima
- Um `MentorshipMatch` liga um aluno a um mentor com score de compatibilidade
- Cada match pode ter várias sessões
- Cada sessão pode gerar feedback entre participantes
