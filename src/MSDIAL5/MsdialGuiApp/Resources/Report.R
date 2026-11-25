# Reporting: writes Notame_Report.Rnw next to the notame outputs and compiles it to
# Notame_Report.pdf with TinyTeX. Uses ppath, report and desc_metaboset from Notame.R.
# Keep this file ASCII: accented letters are written as LaTeX commands.

latex_escape <- function(x) {
  x <- gsub("\\", "\001", x, fixed = TRUE)
  x <- gsub("([#$%&_{}])", "\\\\\\1", x)
  x <- gsub("~", "\\textasciitilde{}", x, fixed = TRUE)
  x <- gsub("^", "\\textasciicircum{}", x, fixed = TRUE)
  gsub("\001", "\\textbackslash{}", x, fixed = TRUE)
}

figure_tex <- function(file, caption) {
  c("\\begin{figure}[htbp]", "\\begin{center}",
    paste0("\\includegraphics[width=1.0\\textwidth]{", file, "}"),
    paste0("\\caption{", caption, "}"),
    "\\end{center}", paste0("\\label{", file, "}"), "\\end{figure}", "\\clearpage\n\n")
}

if (report) {
  old_wd <- setwd(ppath)
  tryCatch({
    usrName <- "By Hanhineva Lab"
    rnwFile <- file("Notame_Report.Rnw", "w")
    header <- c("\\documentclass[a4paper]{article}", "\\usepackage[margin=1.0in]{geometry}",
                "\\usepackage{longtable}", "\\usepackage{graphicx}",
                "<<echo=false>>=", "options(width=60);",
                "@", "\\SweaveOpts{eps=FALSE,pdf=TRUE}", "\\title{Notame preprocessing report}",
                paste("\\author{", usrName, "}", sep = ""),
                "\\begin{document}", "\\parskip=.3cm", "\\maketitle")
    cat(header, file = rnwFile, sep = "\n", append = TRUE)

    # Background information
    descr <- "\\section{About notame}\n
            Notame (NOn-TArgeted MEtabolomics) is an R package designed to preprocess
            LC-MS metabolomics data and provide quality metrics, statistics, and
            results visualizations. The package was originally developed by Anton
            Kl{\\aa}vus at the Kati Hanhineva lab, University of Eastern Finland, and
            it utilizes several other R packages, such as MUVR, to perform the
            data processing. The notame publication, including a description of a
            complete LC-MS workflow, is freely available.
            \\footnote{Kl{\\aa}vus A, Kokla M, Noerman S, et al.
            \\textit{Notame: Workflow for Non-Targeted LC--MS Metabolic Profiling},
            Metabolites. 2020; 10(4):135.}\n"
    cat(descr, file = rnwFile, append = TRUE, sep = "\n")

    # Drift correction
    descr <- ("\\section{Drift correction}\n
                Signal drift is a typical issue in LC-MS metabolomics, affecting sequences
                with 30 to 50 injections or more. It causes a gradual change in the signal
                intensity of the molecular features during the LC-MS analysis, which should
                be corrected for optimal data quality. Notame utilizes information from the
                QC samples to correct the drift. The QC samples are pooled from the biological
                samples and injected before and after the samples as well as after every
                10 to 12 samples in the sequence. Notame performs drift correction by fitting
                a smoothed cubic spline to the signal intensities of the QC samples,
                separately for each molecular feature. The smoothing function prevents
                overfitting the curve in case there are single deviating QC samples.\n
                Please note that at least four QC samples are needed in the dataset to
                perform this step. If your data has less, it is advisable to check the
                PCA instead, where the QC samples should be tightly clustered.\n")
    cat(descr, file = rnwFile, append = TRUE, sep = "\n")
    if (file.exists("drift_cor_report.png")) {
      cat(figure_tex("drift_cor_report.png", "Drift correction of a representative feature"),
          file = rnwFile, append = TRUE, sep = "\n")
    } else {
      descr <- "\\textbf{The selected peak file has fewer than four QC samples, so the drift correction was not made.}"
      cat(descr, file = rnwFile, append = TRUE, sep = "\n")
    }

    # Quality metrics
    descr <- ("\\section{Quality metrics}\n
                The quality of each molecular feature is determined by calculating
                descriptive values according to the recommendations by Broadhurst et al.
                \\footnote{Broadhurst D, Goodacre R, Reinke SN et al.
                \\textit{Guidelines and considerations for the use of system suitability and
                quality control samples in mass spectrometry assays applied in untargeted
                clinical metabolomic studies.},
                Metabolomics 14, 72 (2018).}
                . The following quality criteria should be met to consider the molecular
                feature of good quality: the detection rate in the QC samples is atleast
                70\\%, relative standard deviation (RSD) in the QC samples is less than 0.2
                (20\\%), and the D-ratio is less than 0.4. D-ratio or dispersion ratio is
                the sample standard deviation of the QC samples divided by the sample
                standard deviation of the biological samples. The output data matrix from
                Notame will include a column titled Flag that indicates whether the
                molecular feature was flagged for bad quality and the reason (low QC detection
                or low quality).\n
                The flag for low QC detection or low quality is not intended for automatically
                discarding all such peaks but rather be used as a warning signal to pay close
                attention to the peak (e.g., peak shape, peak area integration, signal-to-noise
                ratio) and to decide yourself whether the feature has sufficient quality for
                reporting. For example, in highly variable sample sets, a true metabolite that
                has detectable levels only in a few samples may dilute below the detection limit
                in the pooled QC samples and result in a flag for low QC detection.\n\n")
    cat(descr, file = rnwFile, append = TRUE, sep = "\n")
    cat(gsub("\n", "\n\n", latex_escape(desc_metaboset), fixed = TRUE),
        file = rnwFile, append = TRUE, sep = "\n")

    # PCA
    descr <- c("\\section{Overview of the data}\n",
               "PCA can give brief overview of the data.")
    cat(descr, file = rnwFile, append = TRUE, sep = "\n")
    if (file.exists("PCA_for_report.png")) {
      cat(figure_tex("PCA_for_report.png", "PCA plot of classes in data"),
          file = rnwFile, append = TRUE, sep = "\n")
    }

    # MUVR
    descr <- ("\\section{Selection of differential molecular features with MUVR}\n
                MUVR (Multivariate methods with Unbiased Variable selection in R) is an
                algorithm developed by Lin Shi and Carl Brunius at the Chalmers University
                of Technology
                \\footnote{Shi L, Westerhuis JA, Ros{\\'e}n J, et al.
                \\textit{Variable selection and validation in multivariate modelling.},
                Bioinformatics. 2019 Mar 15;35(6):972-980.}
                . It chooses a limited number of molecular features, in this case maximum
                20, that differ between the study groups (given in the Class column in
                MS-DIAL project). The algorithm uses a recursive variable selection procedure
                with partial least squares (PLS) and random forest (RF) modelling.\n
                The molecular features chosen by MUVR are accompanied with box plots and
                can be used as a starting point for manual curation of the data and finding
                the relevant metabolites for your study question.")
    cat(descr, file = rnwFile, append = TRUE, sep = "\n")
    if (file.exists("box_plotdpi72.png")) {
      cat(figure_tex("box_plotdpi72.png", "12 most significant features"),
          file = rnwFile, append = TRUE, sep = "\n")
    } else {
      descr <- "\\textbf{MUVR was not performed (it needs two or more classes with at least three samples each); see notame\\_log.txt.}"
      cat(descr, file = rnwFile, append = TRUE, sep = "\n")
    }

    # Finally close the file
    end <- c("\\vspace{5 mm}\n\n--------------------------------\n\n",
             "The report was generated on \\Sexpr{date()}, OS system:",
             "\\Sexpr{Sys.info()['sysname']}, version: \\Sexpr{gsub('#[0-9]+', '', Sys.info()['version'])} .\n",
             "\\end{document}\n\n")
    cat(end, file = rnwFile, append = TRUE)
    close(rnwFile)

    utils::Sweave(file = "Notame_Report.Rnw", encoding = "utf8")
    tinytex::latexmk("Notame_Report.tex")
  }, finally = setwd(old_wd))
}
# END
