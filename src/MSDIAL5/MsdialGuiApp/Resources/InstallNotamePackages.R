# Run the setup with the same R installation that MS-DIAL uses.
install_notame_packages <- function(packages, install_tinytex) {
  needs_install <- function(package) {
    if (package == "notame") {
      too_old <- tryCatch(utils::packageVersion(package) < "0.99.0", error = function(e) TRUE)
      if (too_old) return(TRUE)
    }
    !requireNamespace(package, quietly = TRUE)
  }

  missing <- packages[vapply(packages, needs_install, logical(1))]
  if (length(missing)) {
    lib <- .libPaths()[1L]
    if (file.access(lib, 2L) != 0L) {
      user_libs <- strsplit(Sys.getenv("R_LIBS_USER"), .Platform$path.sep, fixed = TRUE)[[1L]]
      if (!length(user_libs)) stop("No writable R library is configured.", call. = FALSE)
      lib <- path.expand(user_libs[1L])
      dir.create(lib, recursive = TRUE, showWarnings = FALSE)
      if (file.access(lib, 2L) != 0L) stop("Cannot write to R library: ", lib, call. = FALSE)
      .libPaths(c(lib, .libPaths()))
    }

    repos <- getOption("repos")
    if (is.null(repos) || identical(unname(repos["CRAN"]), "@CRAN@")) {
      repos["CRAN"] <- "https://cloud.r-project.org"
    }
    bioc <- intersect(missing, c("notame", "SummarizedExperiment", "BiocParallel", "pcaMethods"))
    cran <- setdiff(missing, c(bioc, "MUVR"))
    if (length(bioc) && !requireNamespace("BiocManager", quietly = TRUE)) cran <- c(cran, "BiocManager")
    if ("MUVR" %in% missing && !requireNamespace("remotes", quietly = TRUE)) cran <- c(cran, "remotes")
    if (length(cran)) utils::install.packages(cran, lib = lib, repos = repos)
    if (length(bioc)) {
      original_options <- options(repos = repos)
      on.exit(options(original_options), add = TRUE)
      BiocManager::install(bioc, lib = lib, update = FALSE, ask = FALSE, force = TRUE)
    }
    if ("MUVR" %in% missing) {
      remotes::install_gitlab("CarlBrunius/MUVR", lib = lib, repos = repos, upgrade = "never", force = TRUE)
    }

    unavailable <- missing[vapply(missing, needs_install, logical(1))]
    if (length(unavailable)) {
      stop("R still cannot load the required versions of: ", paste(unavailable, collapse = ", "), call. = FALSE)
    }
  }
  if (install_tinytex && !nzchar(Sys.which("pdflatex")) && !tinytex::is_tinytex()) {
    tinytex::install_tinytex(force = FALSE, add_path = FALSE)
    if (!tinytex::is_tinytex() && !nzchar(Sys.which("pdflatex"))) {
      stop("TinyTeX is unavailable after setup.", call. = FALSE)
    }
  }
}
