namespace PADocGenerator.Api.Common;

public static class UserMessages
{
    public const string InternalError = "Une erreur interne est survenue.";
    public const string EmptyFlowFile = "Le fichier est vide. Veuillez sélectionner un export JSON de flux Power Automate.";
    public const string InvalidJsonFile = "Le fichier sélectionné n'est pas un fichier JSON valide. Veuillez importer un export JSON de flux Power Automate.";
    public const string InvalidFlowFormat = "Le fichier ne correspond pas au format attendu d'un export de flux Power Automate. Vérifiez que le contenu contient bien des actions et des triggers.";
    public const string FlowImportNotFound = "Flux importé introuvable.";
    public const string DocumentationNotFound = "Documentation introuvable.";
    public const string InvalidFlowForDocumentation = "Ce flux n'a pas passé la validation lors de l'import et ne peut pas être documenté.";
    public const string InvalidOriginFlow = "Le flux d'origine est introuvable ou invalide.";
    public const string InvalidStatus = "Le statut demandé est invalide.";
    public const string InvalidSession = "Votre session est invalide. Veuillez vous reconnecter.";
    public const string DuplicateEmail = "Un compte existe déjà avec cet e-mail.";
    public const string InvalidCredentials = "E-mail ou mot de passe incorrect.";
    public const string VersionNotFound = "Version demandée introuvable.";
    public const string ActiveVersionNotFound = "Version active introuvable pour cette documentation.";
}
