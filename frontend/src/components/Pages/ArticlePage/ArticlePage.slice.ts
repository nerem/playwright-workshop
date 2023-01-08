import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { None, Option, Some } from '@hqoss/monads';
import { Article } from '../../../types/article';
import { Comment } from '../../../types/comment';
import { Profile } from '../../../types/profile';
import { GenericErrors } from "../../../types/error";

export interface CommentSectionState {
  comments: Option<Comment[]>;
  commentBody: string;
  submittingComment: boolean;
  errors: GenericErrors;
}

export interface MetaSectionState {
    submittingFavorite: boolean;
    submittingFollow: boolean;
    deletingArticle: boolean;
}

export interface ArticlePageState {
    article: Option<Article>;
    commentSection: CommentSectionState;
    metaSection: MetaSectionState;
}

const initialState: ArticlePageState = {
  article: None,
  commentSection: {
    comments: None,
    commentBody: '',
    submittingComment: false,
    errors: {},
  },
  metaSection: {
    submittingFavorite: false,
    submittingFollow: false,
    deletingArticle: false,
  },
};

const slice = createSlice({
  name: 'settings',
  initialState,
  reducers: {
    initializeArticlePage: () => initialState,
    loadArticle: (state, { payload: article }: PayloadAction<Article>) => {
      state.article = Some(article);
      state.metaSection.submittingFavorite = false;
    },
    loadComments: (state, { payload: comments }: PayloadAction<Comment[]>) => {
      state.commentSection.comments = Some(comments);
      state.commentSection.commentBody = '';
      state.commentSection.submittingComment = false;
    },
    updateAuthor: (state, { payload: author }: PayloadAction<Profile>) => {
      state.article = state.article.map((article) => ({ ...article, author }));
      state.metaSection.submittingFollow = false;
    },
    startSubmittingFavorite: (state) => {
      state.metaSection.submittingFavorite = true;
    },
    startSubmittingFollow: (state) => {
      state.metaSection.submittingFollow = true;
    },
    updateCommentBody: (state, { payload: commentBody }: PayloadAction<string>) => {
      state.commentSection.commentBody = commentBody;
    },
    startSubmittingComment: (state) => {
      state.commentSection.submittingComment = true;
    },
    updateCommentErrors: (state, { payload: errors }: PayloadAction<GenericErrors>) => {
      state.commentSection.errors = errors;
      state.commentSection.submittingComment = false;
    },
    startDeletingArticle: (state) => {
      state.metaSection.deletingArticle = true;
    },
  },
});

export const {
  initializeArticlePage,
  loadArticle,
  loadComments,
  updateAuthor,
  startSubmittingFavorite,
  startSubmittingFollow,
  updateCommentBody,
  startSubmittingComment,
  updateCommentErrors,
  startDeletingArticle,
} = slice.actions;

export default slice.reducer;
