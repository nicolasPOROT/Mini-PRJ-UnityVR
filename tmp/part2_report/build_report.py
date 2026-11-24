from copy import deepcopy
from pathlib import Path
import shutil

from docx import Document
from docx.enum.table import WD_ALIGN_VERTICAL
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Inches, Pt, RGBColor


REFERENCE = Path(r"C:\Users\Nicolas\OneDrive - Efrei\ing3\ST2VAAS\rapport_nicolasPOROT.docx")
OUTPUT = Path(r"C:\Users\Nicolas\unity_project\Mini-PRJ-UnityVR\output\docx\rapport_nicolasPOROT_part2.docx")

BLUE = "1F3864"
PALE_BLUE = "D9EAF7"
LIGHT_BORDER = "D9E2F3"
GRAY = "404040"


def set_cell_shading(cell, fill):
    tc_pr = cell._tc.get_or_add_tcPr()
    shd = tc_pr.find(qn("w:shd"))
    if shd is None:
        shd = OxmlElement("w:shd")
        tc_pr.append(shd)
    shd.set(qn("w:fill"), fill)


def set_cell_border(cell, color=LIGHT_BORDER, size="6"):
    tc_pr = cell._tc.get_or_add_tcPr()
    borders = tc_pr.first_child_found_in("w:tcBorders")
    if borders is None:
        borders = OxmlElement("w:tcBorders")
        tc_pr.append(borders)
    for name in ("top", "left", "bottom", "right", "insideH", "insideV"):
        node = borders.find(qn(f"w:{name}"))
        if node is None:
            node = OxmlElement(f"w:{name}")
            borders.append(node)
        node.set(qn("w:val"), "single")
        node.set(qn("w:sz"), size)
        node.set(qn("w:color"), color)


def set_cell_margins(cell, top=90, start=90, bottom=90, end=90):
    tc_pr = cell._tc.get_or_add_tcPr()
    mar = tc_pr.first_child_found_in("w:tcMar")
    if mar is None:
        mar = OxmlElement("w:tcMar")
        tc_pr.append(mar)
    for side, value in (("top", top), ("start", start), ("bottom", bottom), ("end", end)):
        node = mar.find(qn(f"w:{side}"))
        if node is None:
            node = OxmlElement(f"w:{side}")
            mar.append(node)
        node.set(qn("w:w"), str(value))
        node.set(qn("w:type"), "dxa")


def set_col_width(cell, width_inches):
    cell.width = Inches(width_inches)
    tc_pr = cell._tc.get_or_add_tcPr()
    tc_w = tc_pr.find(qn("w:tcW"))
    if tc_w is None:
        tc_w = OxmlElement("w:tcW")
        tc_pr.append(tc_w)
    tc_w.set(qn("w:w"), str(int(width_inches * 1440)))
    tc_w.set(qn("w:type"), "dxa")


def write_cell(cell, text, bold=False, color="000000", align=WD_ALIGN_PARAGRAPH.LEFT):
    cell.text = ""
    p = cell.paragraphs[0]
    p.alignment = align
    p.paragraph_format.space_after = Pt(0)
    p.paragraph_format.space_before = Pt(0)
    run = p.add_run(str(text))
    run.font.name = "Calibri"
    run._element.rPr.rFonts.set(qn("w:ascii"), "Calibri")
    run._element.rPr.rFonts.set(qn("w:hAnsi"), "Calibri")
    run.font.size = Pt(9.5)
    run.bold = bold
    run.font.color.rgb = RGBColor.from_string(color)
    cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
    set_cell_margins(cell)
    set_cell_border(cell)


def add_table(doc, headers, rows, widths):
    table = doc.add_table(rows=1, cols=len(headers))
    table.autofit = False
    for i, header in enumerate(headers):
        cell = table.rows[0].cells[i]
        set_col_width(cell, widths[i])
        set_cell_shading(cell, BLUE)
        write_cell(cell, header, bold=True, color="FFFFFF", align=WD_ALIGN_PARAGRAPH.CENTER)
    for row_index, row in enumerate(rows):
        cells = table.add_row().cells
        for i, value in enumerate(row):
            set_col_width(cells[i], widths[i])
            if row_index % 2 == 1:
                set_cell_shading(cells[i], PALE_BLUE)
            alignment = WD_ALIGN_PARAGRAPH.CENTER if len(str(value)) < 26 else WD_ALIGN_PARAGRAPH.LEFT
            write_cell(cells[i], value, align=alignment)
    doc.add_paragraph().paragraph_format.space_after = Pt(1)
    return table


def add_title(doc, text, size, color, bold=True, after=3):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_after = Pt(after)
    r = p.add_run(text)
    r.font.name = "Calibri"
    r._element.rPr.rFonts.set(qn("w:ascii"), "Calibri")
    r._element.rPr.rFonts.set(qn("w:hAnsi"), "Calibri")
    r.font.size = Pt(size)
    r.font.color.rgb = RGBColor.from_string(color)
    r.bold = bold
    return p


def add_body(doc, text):
    p = doc.add_paragraph(style="Normal")
    p.paragraph_format.space_after = Pt(6)
    p.paragraph_format.line_spacing = 1.08
    r = p.add_run(text)
    r.font.name = "Calibri"
    r._element.rPr.rFonts.set(qn("w:ascii"), "Calibri")
    r._element.rPr.rFonts.set(qn("w:hAnsi"), "Calibri")
    r.font.size = Pt(10.5)
    return p


def add_h1(doc, text):
    p = doc.add_paragraph(text, style="Heading 1")
    p.paragraph_format.space_before = Pt(18)
    p.paragraph_format.space_after = Pt(7)
    for run in p.runs:
        run.font.name = "Calibri"
        run._element.rPr.rFonts.set(qn("w:ascii"), "Calibri")
        run._element.rPr.rFonts.set(qn("w:hAnsi"), "Calibri")
        run.font.color.rgb = RGBColor(0, 0, 0)
    return p


def add_h2(doc, text):
    p = doc.add_paragraph(text, style="Heading 2")
    p.paragraph_format.space_before = Pt(12)
    p.paragraph_format.space_after = Pt(5)
    for run in p.runs:
        run.font.name = "Calibri"
        run._element.rPr.rFonts.set(qn("w:ascii"), "Calibri")
        run._element.rPr.rFonts.set(qn("w:hAnsi"), "Calibri")
        run.font.color.rgb = RGBColor(0, 0, 0)
    return p


def add_caption(doc, text):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_after = Pt(8)
    r = p.add_run(text)
    r.italic = True
    r.font.name = "Calibri"
    r.font.size = Pt(9)
    r.font.color.rgb = RGBColor.from_string(GRAY)


def add_capture_slot(doc, label):
    table = doc.add_table(rows=1, cols=1)
    table.autofit = False
    cell = table.cell(0, 0)
    set_col_width(cell, 6.5)
    set_cell_shading(cell, "F2F2F2")
    set_cell_border(cell, color="A6A6A6", size="8")
    cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
    p = cell.paragraphs[0]
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(42)
    p.paragraph_format.space_after = Pt(42)
    r = p.add_run(label)
    r.bold = True
    r.font.name = "Calibri"
    r.font.size = Pt(10.5)
    r.font.color.rgb = RGBColor.from_string(GRAY)


def clear_document_body(doc):
    body = doc._element.body
    sect_pr = body.sectPr
    for child in list(body):
        if child is not sect_pr:
            body.remove(child)


def main():
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(REFERENCE, OUTPUT)
    doc = Document(OUTPUT)
    clear_document_body(doc)

    section = doc.sections[0]
    section.top_margin = Inches(1)
    section.bottom_margin = Inches(0.9)
    section.left_margin = Inches(1)
    section.right_margin = Inches(1)

    add_title(doc, "RAPPORT SYNTHETIQUE", 20, BLUE)
    add_title(doc, "Fruit Ninja VR - Part 2", 16, "000000")
    add_title(doc, "Gestion des ressources et optimisation memoire pour Windows PCVR et Meta Quest", 12, GRAY, bold=False, after=12)

    add_table(doc, ["Information", "Detail"], [
        ["Module", "ST2VAAS - S9 Majeure T2IA - 2026-2027"],
        ["Membre du groupe", "Nicolas POROT"],
        ["Projet", "Fruit Ninja VR - Part 2"],
        ["Moteur", "Unity 6000.0.56f1"],
        ["Package de chargement", "Addressables 2.7.6"],
    ], [2.05, 4.45])

    add_h1(doc, "1. Objectif et perimetre de la partie 2")
    add_body(doc, "Cette seconde partie optimise la gestion des ressources de Fruit Ninja VR. Le travail cible les objets crees frequemment pendant une partie : fruits, bombes et effets de particules. Il introduit des pools d'objets, le chargement Addressables des variantes PCVR et Quest, puis une selection de fruits adaptee au budget de chaque plateforme.")
    add_table(doc, ["Tache", "Realisation dans le projet", "But"], [
        ["Pools de projectiles", "Fruits et bombes reutilises apres un rate ou une coupe", "Eviter les allocations et destructions repetitives"],
        ["Pools d'effets", "Effets de jus et d'explosion relances puis remis en attente", "Limiter les pics lies aux particules"],
        ["Addressables", "Effets et fruits charges a la demande", "Ne charger que les ressources utiles"],
        ["Variantes de plateforme", "Ananas PCVR uniquement ; pomme et banane sur Quest", "Reduire le cout memoire et le temps de chargement Quest"],
    ], [1.45, 3.1, 1.95])

    add_h1(doc, "2. Organisation des pools et reinitialisation")
    add_h2(doc, "2.1 Pool des fruits et des bombes")
    add_body(doc, "ProjectilePool conserve une pile d'instances inactives pour chaque prefab. Lorsqu'un canon tire, il recupere une instance disponible ou en cree une si le pool est vide. Les instances inactives sont rangees sous un objet racine persistant. Cette organisation separe les types de projectiles tout en evitant une taille fixe qui pourrait bloquer une salve exceptionnelle.")
    add_table(doc, ["Evenement", "Traitement", "Etat final"], [
        ["Tir d'un canon", "Recuperation dans le pool ou creation a la demande", "Projectile actif a la position et rotation du canon"],
        ["Entree dans la zone de chute", "FruitFall puis retour dans ProjectilePool", "Projectile desactive et range"],
        ["Coupe d'un fruit ou d'une bombe", "Score ou perte de vie, puis retour du projectile source", "Objet principal reutilisable ; morceaux temporaires supprimes apres 2 s"],
    ], [1.65, 3.35, 1.5])
    add_h2(doc, "2.2 Reinitialisation avant reutilisation")
    add_body(doc, "Avant l'activation et avant le rangement, les vitesses lineaire et angulaire de chaque Rigidbody sont remises a zero. Cette etape evite qu'un fruit reutilise conserve une rotation ou une impulsion de son lancement precedent. Le lanceur applique ensuite une nouvelle force verticale au Rigidbody recupere.")
    add_table(doc, ["Verification", "Methode de controle", "Resultat attendu"], [
        ["Score", "Couper un fruit puis attendre la fin de la fenetre de combo", "Une seule augmentation de score pour une coupe"],
        ["Vie", "Laisser tomber un fruit selon la difficulte puis couper une bombe", "Perte de vie conforme aux regles existantes"],
        ["Reutilisation", "Observer plusieurs cycles tir - chute - tir", "Aucun projectile residuel ni mouvement herite"],
        ["Effets", "Couper plusieurs objets successivement", "Effet visible a chaque coupe sans accumulation persistante"],
    ], [1.45, 3.35, 1.7])

    add_h1(doc, "3. Pool des effets temporaires")
    add_body(doc, "TemporaryEffectPool reutilise les prefabs de particules apres une duree de vie de deux secondes. A chaque lecture, les ParticleSystem enfants sont nettoyes puis relances. Au retour dans le pool, ils sont arretes et vides. Le comportement conserve le feedback visuel de la partie 1 sans instancier ni detruire un prefab d'effet a chaque interaction.")
    add_table(doc, ["Operation", "Effet de jus ou explosion"], [
        ["Lecture", "Positionnement a la coupe, Clear, Play puis activation"],
        ["Fin de vie", "Apres deux secondes, StopEmittingAndClear, desactivation et rangement"],
        ["Cas simultane", "Le pool s'agrandit si plusieurs effets sont encore visibles"],
    ], [2.0, 4.5])

    add_h1(doc, "4. Groupes Addressables et cycle de vie des ressources")
    add_h2(doc, "4.1 Groupes configures")
    add_table(doc, ["Groupe Addressables", "Contenu", "Utilisation"], [
        ["Slice Effects PCVR and Quest", "JuiceFx PCVR et Quest ; Explosion PCVR et Quest", "L'effet adapte est choisi avec la plateforme"],
        ["Fruit Projectiles Shared", "Pomme et banane", "Charge sur PCVR et Quest"],
        ["Fruit Projectiles PCVR", "Ananas", "Charge uniquement hors Android"],
    ], [2.25, 2.55, 1.7])
    add_h2(doc, "4.2 Chargement, utilisation et liberation")
    add_body(doc, "SliceableObject stocke deux AssetReferenceGameObject pour les effets de coupe. Lors de la coupe, le script selectionne la variante Quest sous Android et la variante PCVR sur les autres plateformes. TemporaryEffectPool lance LoadAssetAsync lors de la premiere demande et met les demandes suivantes en attente jusqu'a la fin du chargement. Les handles d'effets sont liberes a la fermeture de l'application.")
    add_body(doc, "CanonShoot charge sa liste de fruits au demarrage : trois references sur PCVR, deux references sur Quest. Les handles associes sont conserves pendant la vie du canon puis liberes dans OnDestroy. Les prefabs charges servent ensuite au ProjectilePool, qui ne charge pas a nouveau un fruit deja disponible.")

    add_h1(doc, "5. Prefab adapte aux plateformes")
    add_body(doc, "Le prefab FruitCanon et les canons presentes dans les zones de jeu possedent maintenant deux listes de references Addressables. La liste PCVR contient pomme, banane et ananas. La liste Quest ne contient que pomme et banane. L'ananas est isole dans un groupe PCVR afin que son prefab plus complexe ne soit pas demande par la version Android pendant la partie.")
    add_table(doc, ["Plateforme", "Fruits charges", "Justification"], [
        ["Windows PCVR", "Pomme, banane, ananas", "Le PC dispose d'un budget processeur et graphique plus large ; la diversite visuelle est conservee"],
        ["Meta Quest autonome", "Pomme, banane", "L'ananas est retire de la selection pour reduire la pression memoire liee au modele plus complexe"],
    ], [1.55, 1.9, 3.05])

    add_h1(doc, "6. Protocole de mesure Part 1 et Part 2")
    add_body(doc, "Le protocole compare une session representative avec une difficulte identique et plusieurs cycles de tirs, de coupes, de ratages et de bombes. La partie 1 sert de base, avec des allocations et destructions repetitives. La partie 2 doit etre mesuree avec les pools actifs et les groupes Addressables configures. Les deux essais doivent etre realises apres un court echauffement, sur la meme scene et avec le meme casque.")
    add_table(doc, ["Indicateur", "Part 1", "Part 2 a relever", "Interpretation attendue"], [
        ["FPS PCVR", "72 FPS releves dans le rapport Part 1", "Capture Profiler / statistiques GPU", "Stabilite au moins equivalente"],
        ["FPS Quest", "64 FPS releves dans le rapport Part 1", "Capture Profiler sur appareil", "Stabilite au moins equivalente"],
        ["GC Alloc par frame", "Non releve dans le rapport Part 1", "Profiler Unity pendant les salves", "Diminution des allocations pendant le jeu"],
        ["Instances creees", "Instantiation et Destroy a chaque cycle", "Profiler Hierarchy et Memory", "Reutilisation des instances apres la premiere salve"],
        ["Memoire des assets", "Non mesuree", "Memory Profiler ou module Memory", "Quest ne charge pas l'ananas pendant la partie"],
    ], [1.35, 1.45, 1.85, 1.85])
    add_h2(doc, "6.1 Captures du Profiler a ajouter apres la mesure")
    add_capture_slot(doc, "CAPTURE A INSERER - Profiler Unity PCVR, Part 1 et Part 2, meme echelle et meme sequence de jeu")
    add_caption(doc, "Figure 1 - Comparaison Profiler PCVR : allocations, temps CPU et nombre d'instances.")
    add_capture_slot(doc, "CAPTURE A INSERER - Profiler Unity Quest, Part 1 et Part 2, meme echelle et meme sequence de jeu")
    add_caption(doc, "Figure 2 - Comparaison Profiler Quest : allocations, temps CPU et memoire chargee.")

    add_h1(doc, "7. Resultats, difficultes et limitations")
    add_h2(doc, "7.1 Etat des tests PCVR et Quest")
    add_body(doc, "La configuration a ete compilee par Unity sans erreur. Les resultats fonctionnels de la partie 1 restent la reference disponible : 72 FPS sur Windows PCVR et 64 FPS sur Quest, sans crash ni blocage. Aucune nouvelle valeur Profiler ou capture de jeu n'a ete fournie pour la partie 2 ; elles doivent etre renseignees avec le protocole de la section 6 avant de conclure sur un gain chiffre.")
    add_table(doc, ["Verification", "PCVR", "Quest"], [
        ["Compilation de la configuration", "Reussie", "Reussie"],
        ["Liste de fruits", "Pomme, banane, ananas", "Pomme, banane"],
        ["Effet de coupe", "Variante PCVR via Addressables", "Variante Quest via Addressables"],
        ["Mesure Profiler Part 2", "A effectuer et capturer", "A effectuer et capturer"],
        ["Resultat FPS Part 2", "A renseigner apres essai", "A renseigner apres essai"],
    ], [2.5, 2.0, 2.0])
    add_h2(doc, "7.2 Difficultes rencontrees et solutions")
    add_table(doc, ["Difficulte", "Solution appliquee", "Limitation restante"], [
        ["Reutiliser sans conserver une physique precedente", "Remise a zero des vitesses et du Rigidbody avant activation et rangement", "Les morceaux generes par EzySlice restent temporaires et sont detruits apres deux secondes"],
        ["Ne pas lancer un effet avant son chargement", "Mise en attente des demandes pendant LoadAssetAsync", "Une premiere apparition peut etre retardee si le chargement est long"],
        ["Conserver une selection differente selon la plateforme", "Deux listes Addressables dans les canons et groupe PCVR isole", "La validation finale depend d'un build et d'un test physique Quest"],
        ["Comparer objectivement les deux parties", "Protocole identique, echauffement et captures a meme echelle", "Les mesures Part 2 restent a collecter"],
    ], [1.75, 2.7, 2.05])

    add_h1(doc, "8. Conclusion")
    add_body(doc, "La partie 2 remplace les cycles repetes d'instanciation et de destruction des projectiles et des effets par des pools extensibles. Les variantes visuelles et les fruits sont charges par Addressables selon la plateforme. Quest utilise ainsi les effets adaptes et ne selectionne pas l'ananas, tandis que PCVR conserve les trois fruits. La derniere etape est de relever les captures Profiler prevues afin de quantifier la baisse d'allocations et de confirmer le comportement sur les deux casques.")

    doc.save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    main()
